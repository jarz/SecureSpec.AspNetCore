#pragma warning disable CA1031 // Do not catch general exception types - intentional for metadata extraction resilience

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Discovers controller-based API endpoints using MVC API Explorer.
/// </summary>
public class ControllerDiscoveryStrategy : IEndpointDiscoveryStrategy
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;
    private readonly DiagnosticsLogger _diagnosticsLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerDiscoveryStrategy"/> class.
    /// </summary>
    /// <param name="actionDescriptorProvider">Provider for action descriptors.</param>
    /// <param name="diagnosticsLogger">Logger for diagnostic events.</param>
    public ControllerDiscoveryStrategy(
        IActionDescriptorCollectionProvider actionDescriptorProvider,
        DiagnosticsLogger diagnosticsLogger)
    {
        _actionDescriptorProvider = actionDescriptorProvider ?? throw new ArgumentNullException(nameof(actionDescriptorProvider));
        _diagnosticsLogger = diagnosticsLogger ?? throw new ArgumentNullException(nameof(diagnosticsLogger));
    }

    /// <inheritdoc />
    public Task<IEnumerable<EndpointMetadata>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new List<EndpointMetadata>();

        // Create defensive copy to prevent thread safety issues
        var actionDescriptors = _actionDescriptorProvider.ActionDescriptors.Items.ToList();

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"Discovering controller endpoints. Found {actionDescriptors.Count} action descriptors.");

        var processedCount = 0;
        foreach (var actionDescriptor in actionDescriptors)
        {
            // Check cancellation every 10 iterations
            if (processedCount % 10 == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            processedCount++;

            // Only process controller actions
            if (actionDescriptor is not ControllerActionDescriptor controllerAction)
            {
                continue;
            }

            try
            {
                var metadata = CreateEndpointMetadata(controllerAction);

                // Validate critical metadata is present
                var actionDisplayName = GetActionDisplayName(controllerAction);
                // RoutePattern can be empty string for root routes with [Route("")]
                // Skip only if null (couldn't build route) or if empty without explicit route attribute
                if (metadata.RoutePattern == null ||
                    (string.IsNullOrEmpty(metadata.RoutePattern) &&
                     controllerAction.AttributeRouteInfo?.Template == null &&
                     !HasExplicitRouteAttribute(controllerAction)))
                {
                    _diagnosticsLogger.LogWarning(
                        DiagnosticCodes.Discovery.MetadataExtractionFailed,
                        $"Controller action {actionDisplayName} has no route pattern. Skipping for safety.");
                    continue;
                }

                if (metadata.HttpMethods.Count == 0)
                {
                    _diagnosticsLogger.LogWarning(
                        DiagnosticCodes.Discovery.MetadataExtractionFailed,
                        $"Controller action {actionDisplayName} has no HTTP methods. Skipping for safety.");
                    continue;
                }

                endpoints.Add(metadata);
            }
            catch (Exception ex)
            {
                var actionDisplayName = GetActionDisplayName(controllerAction);
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.Discovery.MetadataExtractionFailed,
                    $"Failed to extract metadata from controller action {actionDisplayName}: {ex.Message}");
            }
        }

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.Discovery.EndpointsDiscovered,
            $"Controller discovery completed. Discovered {endpoints.Count} endpoints.");

        return Task.FromResult<IEnumerable<EndpointMetadata>>(endpoints);
    }

    private static string GetActionDisplayName(ControllerActionDescriptor actionDescriptor)
    {
        return actionDescriptor.DisplayName ?? $"{actionDescriptor.ControllerName}.{actionDescriptor.ActionName}";
    }

    private static bool HasExplicitRouteAttribute(ControllerActionDescriptor actionDescriptor)
    {
        return actionDescriptor.MethodInfo
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true)
            .Length > 0;
    }

    private EndpointMetadata CreateEndpointMetadata(ControllerActionDescriptor actionDescriptor)
    {
        // Extract all HTTP methods from action descriptor
        var httpMethods = GetHttpMethods(actionDescriptor);

        // Safety check: ensure we have at least one HTTP method
        if (httpMethods.Count == 0)
        {
            httpMethods.Add("GET"); // Default fallback
        }

        // Build route pattern with conventional routing support
        var routePattern = BuildRoutePattern(actionDescriptor);

        return new EndpointMetadata
        {
            HttpMethod = httpMethods[0], // Primary method
            HttpMethods = httpMethods,
            RoutePattern = routePattern,
            OperationName = actionDescriptor.ActionName,
            MethodInfo = actionDescriptor.MethodInfo,
            ControllerType = actionDescriptor.ControllerTypeInfo.AsType(),
            ActionDescriptor = actionDescriptor
        };
    }

    private static List<string> GetHttpMethods(ControllerActionDescriptor actionDescriptor)
    {
        // Use HashSet to prevent duplicates
        var methodsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Check ActionConstraints for HTTP method metadata
        var httpMethodMetadata = actionDescriptor.ActionConstraints?
            .OfType<Microsoft.AspNetCore.Mvc.ActionConstraints.HttpMethodActionConstraint>()
            .FirstOrDefault();

        if (httpMethodMetadata != null && httpMethodMetadata.HttpMethods.Any())
        {
            // Add all HTTP methods from constraints
            foreach (var method in httpMethodMetadata.HttpMethods)
            {
                methodsSet.Add(method);
            }
        }

        // Check for [AcceptVerbs] attribute
        var acceptVerbsAttribute = actionDescriptor.MethodInfo
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AcceptVerbsAttribute), true)
            .FirstOrDefault() as Microsoft.AspNetCore.Mvc.AcceptVerbsAttribute;

        if (acceptVerbsAttribute?.HttpMethods != null)
        {
            try
            {
                var verbs = acceptVerbsAttribute.HttpMethods.ToList();
                foreach (var verb in verbs)
                {
                    methodsSet.Add(verb);
                }
            }
            catch
            {
                // If HttpMethods is not enumerable as expected, continue to next detection method
            }
        }

        // Infer from HTTP verb attributes (including custom ones inheriting from HttpMethodAttribute)
        // Use inherit:false to prevent base class HTTP methods from being inherited
        var methodInfo = actionDescriptor.MethodInfo;
        var httpMethodAttributes = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute), false)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>();

        var methodsFromAttributes = httpMethodAttributes
            .Where(attr => attr.HttpMethods != null)
            .SelectMany(attr => attr.HttpMethods);

        foreach (var method in methodsFromAttributes)
        {
            methodsSet.Add(method);
        }

        // If we found explicit methods from any source, return them
        if (methodsSet.Count > 0)
        {
            return SortHttpMethods(methodsSet);
        }

        // Infer from action name convention (GetUsers, PostOrder, etc.)
        var actionName = actionDescriptor.ActionName;
        var conventionalVerbs = new[] { "Get", "Post", "Put", "Delete", "Patch" };

        var matchedVerb = conventionalVerbs.FirstOrDefault(verb =>
            actionName.StartsWith(verb, StringComparison.OrdinalIgnoreCase));

        if (matchedVerb != null)
        {
            methodsSet.Add(matchedVerb.ToUpperInvariant());
            return SortHttpMethods(methodsSet);
        }

        // Default to GET if no method specified
        methodsSet.Add("GET");
        return SortHttpMethods(methodsSet);
    }

    private static readonly Dictionary<string, int> HttpMethodOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GET"] = 0,
        ["POST"] = 1,
        ["PUT"] = 2,
        ["PATCH"] = 3,
        ["DELETE"] = 4,
        ["HEAD"] = 5,
        ["OPTIONS"] = 6,
        ["TRACE"] = 7,
        ["CONNECT"] = 8
    };

    private static List<string> SortHttpMethods(HashSet<string> methodsSet)
    {
        // Sort HTTP methods in a standard order for consistency using dictionary lookup
        return methodsSet
            .OrderBy(m => HttpMethodOrder.TryGetValue(m, out var order) ? order : int.MaxValue)
            .ThenBy(m => m, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildRoutePattern(ControllerActionDescriptor actionDescriptor)
    {
        // Prefer attribute routing if available and not empty
        if (!string.IsNullOrWhiteSpace(actionDescriptor.AttributeRouteInfo?.Template))
        {
            return actionDescriptor.AttributeRouteInfo.Template;
        }

        // Check for action-level [Route] attribute
        var actionRouteAttribute = GetActionRouteAttribute(actionDescriptor);

        // Check if action route is absolute (starts with / or ~/) - ignores controller route
        var absoluteRoute = TryGetAbsoluteRoute(actionRouteAttribute, actionDescriptor);
        if (absoluteRoute != null)
        {
            return absoluteRoute;
        }

        var parts = new List<string>();
        AddControllerRouteParts(actionDescriptor, parts);

        AddActionRouteParts(actionDescriptor, actionRouteAttribute, parts);

        AddRouteParameters(actionDescriptor, parts);

        // Filter out empty parts and build the route pattern
        var cleanedParts = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        return cleanedParts.Count > 0 ? string.Join("/", cleanedParts) : string.Empty;
    }

    private static Microsoft.AspNetCore.Mvc.RouteAttribute? GetActionRouteAttribute(ControllerActionDescriptor actionDescriptor)
    {
        // ASP.NET Core supports multiple [Route] attributes on an action
        // We prioritize based on: absolute routes first, then first defined
        var routeAttributes = actionDescriptor.MethodInfo
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true)
            .OfType<Microsoft.AspNetCore.Mvc.RouteAttribute>()
            .ToList();

        if (routeAttributes.Count == 0)
        {
            return null;
        }

        // Prioritize absolute routes (starting with / or ~/)
        var absoluteRoute = routeAttributes.FirstOrDefault(r =>
            !string.IsNullOrWhiteSpace(r.Template) &&
            (r.Template.TrimStart().StartsWith('/') || r.Template.TrimStart().StartsWith("~/", StringComparison.Ordinal)));

        return absoluteRoute ?? routeAttributes[0];
    }

    private static string? TryGetAbsoluteRoute(Microsoft.AspNetCore.Mvc.RouteAttribute? actionRouteAttribute, ControllerActionDescriptor actionDescriptor)
    {
        if (actionRouteAttribute == null || string.IsNullOrWhiteSpace(actionRouteAttribute.Template))
        {
            return null;
        }

        var actionTemplate = actionRouteAttribute.Template.TrimStart();
        if (!string.IsNullOrWhiteSpace(actionTemplate) &&
            (actionTemplate.StartsWith('/') || actionTemplate.StartsWith("~/", StringComparison.Ordinal)))
        {
            // Trim once and reuse - validate not empty after trimming
            var cleanTemplate = actionTemplate.TrimStart('~', '/');
            if (string.IsNullOrWhiteSpace(cleanTemplate))
            {
                // Route was just "/" or "~/" - treat as root route
                return string.Empty;
            }
            return ReplaceRouteTokens(cleanTemplate, actionDescriptor);
        }

        return null;
    }

    private static void AddControllerRouteParts(ControllerActionDescriptor actionDescriptor, List<string> parts)
    {
        // Check for controller-level [Route] attribute
        var controllerRouteAttribute = actionDescriptor.ControllerTypeInfo
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), true)
            .FirstOrDefault() as Microsoft.AspNetCore.Mvc.RouteAttribute;

        // Add controller-level route prefix if present
        if (!string.IsNullOrWhiteSpace(controllerRouteAttribute?.Template))
        {
            var template = ReplaceRouteTokens(controllerRouteAttribute.Template, actionDescriptor);
            if (!string.IsNullOrWhiteSpace(template))
            {
                parts.Add(template.Trim('/'));
            }
        }
        else
        {
            // Build conventional route pattern
            var routeValues = actionDescriptor.RouteValues;

            // Add area if present (null-safe access)
            if (routeValues != null &&
                routeValues.TryGetValue("area", out var area) &&
                !string.IsNullOrEmpty(area))
            {
                parts.Add(area);
            }

            // Add controller name
            var controllerName = actionDescriptor.ControllerName;
            if (!string.IsNullOrEmpty(controllerName))
            {
                parts.Add(controllerName);
            }
        }
    }

    private static void AddActionRouteParts(ControllerActionDescriptor actionDescriptor, Microsoft.AspNetCore.Mvc.RouteAttribute? actionRouteAttribute, List<string> parts)
    {
        // Add action-level route if present (non-absolute)
        if (actionRouteAttribute != null && !string.IsNullOrWhiteSpace(actionRouteAttribute.Template))
        {
            var actionTemplate = actionRouteAttribute.Template.TrimStart();
            if (!string.IsNullOrWhiteSpace(actionTemplate) &&
                !actionTemplate.StartsWith('/') && !actionTemplate.StartsWith("~/", StringComparison.Ordinal))
            {
                var template = ReplaceRouteTokens(actionTemplate, actionDescriptor);
                if (!string.IsNullOrWhiteSpace(template))
                {
                    parts.Add(template.Trim('/'));
                }
            }
        }
        else
        {
            // Add action name (skip if it's "Index" for conventional routing)
            var actionName = actionDescriptor.ActionName;
            if (!string.IsNullOrEmpty(actionName) &&
                !string.Equals(actionName, "Index", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(actionName);
            }
        }
    }

    private static void AddRouteParameters(ControllerActionDescriptor actionDescriptor, List<string> parts)
    {
        // Build current route pattern to check for existing parameters
        var currentRoute = string.Join("/", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        var existingParams = ExtractRouteParameters(currentRoute);

        // Add route parameters from action method
        if (actionDescriptor.Parameters == null)
        {
            return;
        }

        var parameters = actionDescriptor.Parameters
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerParameterDescriptor>()
            .Where(p => ShouldIncludeAsRouteParameter(p, existingParams))
            .OrderBy(p => p.ParameterInfo.Position)
            .ToList();

        foreach (var param in parameters)
        {
            // Defensive null check for parameter name
            if (param?.Name == null)
            {
                continue;
            }

            var paramName = param.Name;
            if (!string.IsNullOrEmpty(paramName) && !existingParams.Contains(paramName))
            {
                var isOptional = param.ParameterInfo.HasDefaultValue;
                parts.Add(isOptional ? $"{{{paramName}?}}" : $"{{{paramName}}}");
            }
        }
    }

    private static HashSet<string> ExtractRouteParameters(string route)
    {
        var parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        while (index < route.Length)
        {
            var startIndex = route.IndexOf('{', index);
            if (startIndex == -1)
            {
                break;
            }

            // Check for escaped braces {{ - skip them
            if (startIndex + 1 < route.Length && route[startIndex + 1] == '{')
            {
                index = startIndex + 2;
                continue;
            }

            // Find matching closing brace, accounting for nested braces and escaped braces
            var endIndex = FindClosingBrace(route, startIndex);

            if (endIndex > startIndex)
            {
                var paramText = route.Substring(startIndex + 1, endIndex - startIndex - 1);
                var paramName = ExtractParameterName(paramText);

                if (!string.IsNullOrEmpty(paramName))
                {
                    parameters.Add(paramName);
                }
                index = endIndex + 1;
            }
            else
            {
                // Malformed template, skip
                break;
            }
        }

        return parameters;
    }

    private static string ExtractParameterName(string paramText)
    {
        // Remove optional marker and extract parameter name before colon or equals
        var cleanText = paramText.TrimEnd('?');

        // Find first unescaped separator - handle constraints like {id:int} or {id:regex(^[0-9]{3}$)}
        var paramName = cleanText;
        var depth = 0;
        for (var i = 0; i < cleanText.Length; i++)
        {
            var ch = cleanText[i];
            if (ch == '(' || ch == '[' || ch == '{')
            {
                depth++;
            }
            else if (ch == ')' || ch == ']' || ch == '}')
            {
                depth--;
                // Malformed constraint - unmatched closing bracket
                if (depth < 0)
                {
                    break;
                }
            }
            else if (depth == 0 && (ch == ':' || ch == '='))
            {
                paramName = cleanText.Substring(0, i);
                break;
            }
        }

        // If depth is not 0, constraint is malformed - still return best guess
        return paramName.Trim();
    }

    private static int FindClosingBrace(string route, int startIndex)
    {
        var braceCount = 1;
        var index = startIndex + 1;

        while (index < route.Length && braceCount > 0)
        {
            var currentChar = route[index];
            var isEscaped = index + 1 < route.Length && route[index + 1] == currentChar;

            if (isEscaped && (currentChar == '{' || currentChar == '}'))
            {
                index += 2;
                continue;
            }

            if (currentChar == '}')
            {
                braceCount--;
            }
            else if (currentChar == '{')
            {
                braceCount++;
            }

            index++;
        }

        return braceCount == 0 ? index - 1 : -1;
    }

    private static bool ShouldIncludeAsRouteParameter(Microsoft.AspNetCore.Mvc.Controllers.ControllerParameterDescriptor param, HashSet<string> existingParams)
    {
        // Include if explicitly marked with [FromRoute]
        if (param.BindingInfo?.BindingSource?.Id == "Path")
        {
            return true;
        }

        // Include simple types without explicit binding source (conventional routing)
        if (param.BindingInfo?.BindingSource == null && IsSimpleType(param.ParameterType))
        {
            // Only include if not already in route and not a common query parameter name
            var commonQueryParams = new[]
            {
                "page", "pageSize", "pagesize", "limit", "offset", "skip", "take",
                "sort", "sortBy", "sortby", "order", "orderBy", "orderby",
                "filter", "search", "q", "query",
                "include", "expand", "fields", "select",
                "count", "top", "from", "to"
            };
            return !existingParams.Contains(param.Name) &&
                   !commonQueryParams.Contains(param.Name, StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string ReplaceRouteTokens(string template, ControllerActionDescriptor actionDescriptor)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return template;
        }

        var result = template;
        var routeValues = actionDescriptor.RouteValues;

        // Use unique placeholders to prevent replacement collisions
        const string areaPlaceholder = "__AREA_TOKEN_PLACEHOLDER__";
        const string controllerPlaceholder = "__CONTROLLER_TOKEN_PLACEHOLDER__";
        const string actionPlaceholder = "__ACTION_TOKEN_PLACEHOLDER__";

        // First pass: replace only route tokens (in square brackets), not route parameters (in curly braces)
        // This prevents replacing {controller} parameters with the actual controller name
        result = ReplaceTokenCaseInsensitive(result, "[area]", areaPlaceholder);
        result = ReplaceTokenCaseInsensitive(result, "[controller]", controllerPlaceholder);
        result = ReplaceTokenCaseInsensitive(result, "[action]", actionPlaceholder);

        // Second pass: replace placeholders with actual values
        if (routeValues != null && routeValues.TryGetValue("area", out var area) && !string.IsNullOrEmpty(area))
        {
            result = result.Replace(areaPlaceholder, area, StringComparison.Ordinal);
        }
        else
        {
            result = result.Replace(areaPlaceholder, string.Empty, StringComparison.Ordinal);
        }

        var controllerName = !string.IsNullOrEmpty(actionDescriptor.ControllerName) ? actionDescriptor.ControllerName : string.Empty;
        result = result.Replace(controllerPlaceholder, controllerName, StringComparison.Ordinal);

        var actionName = !string.IsNullOrEmpty(actionDescriptor.ActionName) ? actionDescriptor.ActionName : string.Empty;
        result = result.Replace(actionPlaceholder, actionName, StringComparison.Ordinal);

        // Clean up any double slashes that resulted from empty token replacements
        while (result.Contains("//", StringComparison.Ordinal))
        {
            result = result.Replace("//", "/", StringComparison.Ordinal);
        }

        // Remove leading slash if it exists and isn't the only character
        if (result.Length > 1 && result.StartsWith('/'))
        {
            result = result.Substring(1);
        }

        return result;
    }

    private static string ReplaceTokenCaseInsensitive(string input, string token, string replacement)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(token))
        {
            return input;
        }

        // Pre-allocate capacity based on estimated token count
        var estimatedTokenCount = Math.Max(3, input.Length / Math.Max(token.Length * 2, 10));
        var estimatedCapacity = input.Length + ((replacement.Length - token.Length) * estimatedTokenCount);
        var result = new System.Text.StringBuilder(input, Math.Max(input.Length, estimatedCapacity));
        var searchIndex = 0;
        var tokenLength = token.Length;
        var replacementLength = replacement.Length;

        while (searchIndex <= result.Length - tokenLength)
        {
            // Check bounds before each comparison since result.Length changes after modifications
            if (searchIndex + tokenLength > result.Length)
            {
                break;
            }

            var found = true;
            // Use ordinal comparison to avoid culture-specific issues (e.g., Turkish I problem)
            for (var i = 0; i < tokenLength; i++)
            {
                if (!char.Equals(char.ToUpperInvariant(result[searchIndex + i]), char.ToUpperInvariant(token[i])))
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                result.Remove(searchIndex, tokenLength);
                result.Insert(searchIndex, replacement);
                // Move past the replacement to avoid re-matching within it,
                // but ensure we don't skip potential overlapping tokens
                searchIndex += Math.Max(1, replacementLength);
            }
            else
            {
                searchIndex++;
            }
        }

        return result.ToString();
    }

    private static bool IsSimpleType(Type type)
    {
        // Fast path: check common types without allocation
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid) ||
            type == typeof(DateOnly) ||
            type == typeof(TimeOnly) ||
            type == typeof(Uri) ||
            type == typeof(Version))
        {
            return true;
        }

        // Handle nullable types (also common, still no allocation needed)
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            // Recurse once for nullable - check all simple types
            return underlyingType.IsPrimitive ||
                   underlyingType.IsEnum ||
                   underlyingType == typeof(decimal) ||
                   underlyingType == typeof(DateTime) ||
                   underlyingType == typeof(DateTimeOffset) ||
                   underlyingType == typeof(TimeSpan) ||
                   underlyingType == typeof(Guid) ||
                   underlyingType == typeof(DateOnly) ||
                   underlyingType == typeof(TimeOnly) ||
                   underlyingType == typeof(Uri) ||
                   underlyingType == typeof(Version);
        }

        return false;
    }
}
