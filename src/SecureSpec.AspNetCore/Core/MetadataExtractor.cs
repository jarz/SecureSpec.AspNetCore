#pragma warning disable CA1031 // Do not catch general exception types - intentional for metadata extraction resilience

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;

namespace SecureSpec.AspNetCore.Core;

/// <summary>
/// Extracts metadata from endpoint methods, parameters, and attributes.
/// </summary>
public class MetadataExtractor
{
    private readonly SchemaGenerator _schemaGenerator;
    private readonly DiagnosticsLogger _diagnosticsLogger;
    private readonly Dictionary<Assembly, XDocument?> _xmlDocCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataExtractor"/> class.
    /// </summary>
    /// <param name="schemaGenerator">Schema generator for creating parameter and response schemas.</param>
    /// <param name="diagnosticsLogger">Logger for diagnostic events.</param>
    public MetadataExtractor(
        SchemaGenerator schemaGenerator,
        DiagnosticsLogger diagnosticsLogger)
    {
        _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
        _diagnosticsLogger = diagnosticsLogger ?? throw new ArgumentNullException(nameof(diagnosticsLogger));
    }

    /// <summary>
    /// Enriches endpoint metadata by extracting information from method, parameters, and attributes.
    /// </summary>
    /// <param name="metadata">The endpoint metadata to enrich.</param>
    public void EnrichMetadata(EndpointMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.MethodInfo == null)
        {
            return;
        }

        try
        {
            ExtractSummaryAndDescription(metadata);
            ExtractTags(metadata);
            ExtractParameters(metadata);
            ExtractRequestBody(metadata);
            ExtractResponses(metadata);
            ExtractDeprecationStatus(metadata);
            ExtractOperationId(metadata);
        }
        catch (Exception ex)
        {
            _diagnosticsLogger.LogError(
                DiagnosticCodes.Discovery.MetadataExtractionFailed,
                $"Failed to enrich metadata for {metadata.MethodInfo.Name}: {ex.Message}");
        }
    }

    private void ExtractSummaryAndDescription(EndpointMetadata metadata)
    {
        var methodInfo = metadata.MethodInfo!;

        // Try to get XML documentation first
        var xmlDoc = GetXmlDocumentation(methodInfo);
        if (xmlDoc != null)
        {
            metadata.Summary = xmlDoc.Summary;
            metadata.Description = xmlDoc.Remarks ?? xmlDoc.Summary;
        }

        // Check for description attributes (override XML if present)
        var descriptionAttr = methodInfo.GetCustomAttribute<DescriptionAttribute>();
        if (descriptionAttr != null)
        {
            metadata.Description = descriptionAttr.Description;
        }
    }

    private void ExtractTags(EndpointMetadata metadata)
    {
        // Use controller name as default tag for controller endpoints
        if (metadata.ControllerType != null)
        {
            var controllerName = metadata.ControllerType.Name;
            if (controllerName.EndsWith("Controller", StringComparison.Ordinal))
            {
                controllerName = controllerName[..^10]; // Remove "Controller" suffix
            }
            metadata.Tags.Add(controllerName);
        }

        // TODO: Support custom tag attributes when defined
    }

    private void ExtractParameters(EndpointMetadata metadata)
    {
        var methodInfo = metadata.MethodInfo!;
        var parameters = methodInfo.GetParameters();

        foreach (var parameter in parameters)
        {
            // Skip special parameters
            if (IsSpecialParameter(parameter))
            {
                continue;
            }

            var parameterLocation = GetParameterLocation(parameter);

            // FromBody parameters are handled separately as request body
            if (parameterLocation == ParameterLocation.Body)
            {
                continue;
            }

            var openApiParameter = CreateOpenApiParameter(parameter, parameterLocation);
            metadata.Parameters.Add(openApiParameter);
        }
    }

    private void ExtractRequestBody(EndpointMetadata metadata)
    {
        var methodInfo = metadata.MethodInfo!;
        var parameters = methodInfo.GetParameters();

        // Find [FromBody] parameter
        var bodyParameter = parameters.FirstOrDefault(p =>
            p.GetCustomAttribute<FromBodyAttribute>() != null);

        if (bodyParameter != null)
        {
            metadata.RequestBody = CreateRequestBody(bodyParameter);
        }
    }

    private void ExtractResponses(EndpointMetadata metadata)
    {
        var methodInfo = metadata.MethodInfo!;

        // Extract from ProducesResponseType attributes
        var responseTypeAttributes = methodInfo.GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();

        if (responseTypeAttributes.Count > 0)
        {
            foreach (var attr in responseTypeAttributes)
            {
                var statusCode = attr.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var response = CreateOpenApiResponse(attr.Type, statusCode);
                metadata.Responses[statusCode] = response;
            }
        }
        else
        {
            // Default 200 OK response
            var returnType = methodInfo.ReturnType;

            // Unwrap Task<T> or ValueTask<T>
            if (returnType.IsGenericType)
            {
                var genericTypeDef = returnType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(Task<>) || genericTypeDef == typeof(ValueTask<>))
                {
                    returnType = returnType.GetGenericArguments()[0];
                }
            }

            // Skip void, Task, ValueTask, IActionResult, ActionResult
            if (returnType != typeof(void) &&
                returnType != typeof(Task) &&
                returnType != typeof(ValueTask) &&
                !typeof(IActionResult).IsAssignableFrom(returnType))
            {
                var response = CreateOpenApiResponse(returnType, "200");
                metadata.Responses["200"] = response;
            }
            else
            {
                // Default empty 200 response
                metadata.Responses["200"] = new OpenApiResponse
                {
                    Description = "Success"
                };
            }
        }
    }

    private void ExtractDeprecationStatus(EndpointMetadata metadata)
    {
        var methodInfo = metadata.MethodInfo!;
        metadata.Deprecated = methodInfo.GetCustomAttribute<ObsoleteAttribute>() != null;
    }

    private void ExtractOperationId(EndpointMetadata metadata)
    {
        // Use method name as default operation ID if not already set
        if (string.IsNullOrEmpty(metadata.OperationId) && metadata.MethodInfo != null)
        {
            metadata.OperationId = metadata.MethodInfo.Name;
        }
    }

    private static bool IsSpecialParameter(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;

        // Skip CancellationToken, HttpContext, etc.
        return parameterType == typeof(CancellationToken) ||
               parameterType.Name.Contains("HttpContext", StringComparison.Ordinal) ||
               parameterType.Name.Contains("HttpRequest", StringComparison.Ordinal) ||
               parameterType.Name.Contains("HttpResponse", StringComparison.Ordinal);
    }

    private static ParameterLocation GetParameterLocation(ParameterInfo parameter)
    {
        // Check for binding attributes
        if (parameter.GetCustomAttribute<FromQueryAttribute>() != null)
        {
            return ParameterLocation.Query;
        }

        if (parameter.GetCustomAttribute<FromRouteAttribute>() != null)
        {
            return ParameterLocation.Path;
        }

        if (parameter.GetCustomAttribute<FromHeaderAttribute>() != null)
        {
            return ParameterLocation.Header;
        }

        if (parameter.GetCustomAttribute<FromBodyAttribute>() != null)
        {
            return ParameterLocation.Body;
        }

        // Default: simple types go to query, complex types to body
        if (IsSimpleType(parameter.ParameterType))
        {
            return ParameterLocation.Query;
        }

        return ParameterLocation.Body;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               Nullable.GetUnderlyingType(type) != null;
    }

    private OpenApiParameter CreateOpenApiParameter(ParameterInfo parameter, ParameterLocation location)
    {
        var schema = _schemaGenerator.GenerateSchema(parameter.ParameterType);

        var openApiParameter = new OpenApiParameter
        {
            Name = parameter.Name ?? "unknown",
            In = location switch
            {
                ParameterLocation.Query => Microsoft.OpenApi.Models.ParameterLocation.Query,
                ParameterLocation.Path => Microsoft.OpenApi.Models.ParameterLocation.Path,
                ParameterLocation.Header => Microsoft.OpenApi.Models.ParameterLocation.Header,
                _ => Microsoft.OpenApi.Models.ParameterLocation.Query
            },
            Required = location == ParameterLocation.Path || !IsOptionalParameter(parameter),
            Schema = schema
        };

        // Extract description from XML documentation
        var xmlDoc = GetXmlDocumentation(parameter);
        if (xmlDoc != null)
        {
            openApiParameter.Description = xmlDoc.Summary;
        }

        return openApiParameter;
    }

    private OpenApiRequestBody CreateRequestBody(ParameterInfo parameter)
    {
        var schema = _schemaGenerator.GenerateSchema(parameter.ParameterType);

        var requestBody = new OpenApiRequestBody
        {
            Required = !IsOptionalParameter(parameter),
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = schema
                }
            }
        };

        // Extract description from XML documentation
        var xmlDoc = GetXmlDocumentation(parameter);
        if (xmlDoc != null)
        {
            requestBody.Description = xmlDoc.Summary;
        }

        return requestBody;
    }

    private OpenApiResponse CreateOpenApiResponse(Type? responseType, string statusCode)
    {
        var response = new OpenApiResponse
        {
            Description = GetDefaultResponseDescription(statusCode)
        };

        if (responseType != null && responseType != typeof(void))
        {
            var schema = _schemaGenerator.GenerateSchema(responseType);
            response.Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = schema
                }
            };
        }

        return response;
    }

    private static string GetDefaultResponseDescription(string statusCode)
    {
        return statusCode switch
        {
            "200" => "Success",
            "201" => "Created",
            "204" => "No Content",
            "400" => "Bad Request",
            "401" => "Unauthorized",
            "403" => "Forbidden",
            "404" => "Not Found",
            "500" => "Internal Server Error",
            _ => $"Response {statusCode}"
        };
    }

    private static bool IsOptionalParameter(ParameterInfo parameter)
    {
        return parameter.IsOptional ||
               parameter.HasDefaultValue ||
               Nullable.GetUnderlyingType(parameter.ParameterType) != null ||
               !parameter.ParameterType.IsValueType;
    }

    private XmlDocumentation? GetXmlDocumentation(MemberInfo member)
    {
        var assembly = member.DeclaringType?.Assembly;
        if (assembly == null)
        {
            return null;
        }

        if (!_xmlDocCache.TryGetValue(assembly, out var xmlDoc))
        {
            xmlDoc = LoadXmlDocumentation(assembly);
            _xmlDocCache[assembly] = xmlDoc;
        }

        if (xmlDoc == null)
        {
            return null;
        }

        var memberName = GetXmlMemberName(member);
        var memberElement = xmlDoc.Descendants("member")
            .FirstOrDefault(e => e.Attribute("name")?.Value == memberName);

        if (memberElement == null)
        {
            return null;
        }

        return new XmlDocumentation
        {
            Summary = memberElement.Element("summary")?.Value.Trim(),
            Remarks = memberElement.Element("remarks")?.Value.Trim()
        };
    }

    private XmlDocumentation? GetXmlDocumentation(ParameterInfo parameter)
    {
        var method = parameter.Member as MethodInfo;
        if (method == null)
        {
            return null;
        }

        var assembly = method.DeclaringType?.Assembly;
        if (assembly == null)
        {
            return null;
        }

        if (!_xmlDocCache.TryGetValue(assembly, out var xmlDoc))
        {
            xmlDoc = LoadXmlDocumentation(assembly);
            _xmlDocCache[assembly] = xmlDoc;
        }

        if (xmlDoc == null)
        {
            return null;
        }

        var memberName = GetXmlMemberName(method);
        var memberElement = xmlDoc.Descendants("member")
            .FirstOrDefault(e => e.Attribute("name")?.Value == memberName);

        if (memberElement == null)
        {
            return null;
        }

        var paramElement = memberElement.Elements("param")
            .FirstOrDefault(e => e.Attribute("name")?.Value == parameter.Name);

        if (paramElement == null)
        {
            return null;
        }

        return new XmlDocumentation
        {
            Summary = paramElement.Value.Trim()
        };
    }

    private XDocument? LoadXmlDocumentation(Assembly assembly)
    {
        try
        {
            // Try to find XML documentation file
            var assemblyLocation = assembly.Location;
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                return null;
            }

            var xmlPath = Path.ChangeExtension(assemblyLocation, ".xml");
            if (File.Exists(xmlPath))
            {
                return XDocument.Load(xmlPath);
            }

            // TODO: Support explicit XML documentation paths from configuration
        }
        catch (Exception ex)
        {
            _diagnosticsLogger.LogWarning(
                "XML001",
                $"Failed to load XML documentation for {assembly.GetName().Name}: {ex.Message}");
        }

        return null;
    }

    private static string GetXmlMemberName(MemberInfo member)
    {
        var prefix = member.MemberType switch
        {
            MemberTypes.Method => "M:",
            MemberTypes.Property => "P:",
            MemberTypes.Field => "F:",
            MemberTypes.TypeInfo or MemberTypes.NestedType => "T:",
            _ => "M:"
        };

        var declaringType = member.DeclaringType;
        if (declaringType == null)
        {
            return $"{prefix}{member.Name}";
        }

        var fullName = $"{declaringType.FullName}.{member.Name}";

        // Add parameters for methods
        if (member is MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
            {
                var paramTypes = string.Join(",", parameters.Select(p => p.ParameterType.FullName));
                fullName = $"{fullName}({paramTypes})";
            }
        }

        return $"{prefix}{fullName}";
    }

    private enum ParameterLocation
    {
        Query,
        Path,
        Header,
        Body
    }

    private sealed class XmlDocumentation
    {
        public string? Summary { get; set; }
        public string? Remarks { get; set; }
    }
}
