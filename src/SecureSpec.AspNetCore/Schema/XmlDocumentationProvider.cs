using SecureSpec.AspNetCore.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace SecureSpec.AspNetCore.Schema;

/// <summary>
/// Provides XML documentation comments from multiple documentation files with ordered merge support.
/// </summary>
public class XmlDocumentationProvider
{
    private readonly Dictionary<string, XmlMemberDocumentation> _memberDocumentation = new();
    private readonly DiagnosticsLogger _logger;
    private readonly List<string> _loadedFiles = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocumentationProvider"/> class.
    /// </summary>
    /// <param name="logger">The diagnostics logger.</param>
    public XmlDocumentationProvider(DiagnosticsLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads XML documentation from the specified file path.
    /// Files are loaded in order, with later files overwriting earlier ones on conflicts.
    /// </summary>
    /// <param name="xmlPath">The path to the XML documentation file.</param>
    public void LoadXmlDocumentation(string xmlPath)
    {
        ArgumentNullException.ThrowIfNull(xmlPath);

        if (!File.Exists(xmlPath))
        {
            _logger.Log(DiagnosticLevel.Warn, "XML001",
                $"XML documentation file not found: {xmlPath}");
            return;
        }

        try
        {
            var doc = XDocument.Load(xmlPath);
            var members = doc.Descendants("member").ToList();

            foreach (var member in members)
            {
                var nameAttr = member.Attribute("name");
                if (nameAttr == null)
                    continue;

                var memberName = nameAttr.Value;
                var documentation = ParseMemberDocumentation(member);

                // Check for conflicts - if this member was already loaded from a previous file
                if (_memberDocumentation.ContainsKey(memberName))
                {
                    _logger.Log(DiagnosticLevel.Info, "XML002",
                        $"XML documentation conflict: Member '{memberName}' redefined in '{xmlPath}'. Later definition takes precedence.",
                        new Dictionary<string, object>
                        {
                            ["MemberName"] = memberName,
                            ["XmlFile"] = xmlPath,
                            ["PreviousFile"] = _loadedFiles.LastOrDefault(f => f != xmlPath) ?? "unknown"
                        });
                }

                // Last file wins - ordered merge
                _memberDocumentation[memberName] = documentation;
            }

            _loadedFiles.Add(xmlPath);

            _logger.Log(DiagnosticLevel.Info, "XML003",
                $"Loaded XML documentation from '{xmlPath}' with {members.Count} members");
        }
        catch (XmlException ex)
        {
            _logger.Log(DiagnosticLevel.Error, "XML004",
                $"Failed to load XML documentation from '{xmlPath}': {ex.Message}",
                new Dictionary<string, object>
                {
                    ["XmlFile"] = xmlPath,
                    ["Exception"] = ex.GetType().Name,
                    ["Message"] = ex.Message
                });
        }
        catch (IOException ex)
        {
            _logger.Log(DiagnosticLevel.Error, "XML004",
                $"Failed to load XML documentation from '{xmlPath}': {ex.Message}",
                new Dictionary<string, object>
                {
                    ["XmlFile"] = xmlPath,
                    ["Exception"] = ex.GetType().Name,
                    ["Message"] = ex.Message
                });
        }
    }

    /// <summary>
    /// Gets the documentation for a type.
    /// </summary>
    /// <param name="type">The type to get documentation for.</param>
    /// <returns>The type documentation, or null if not found.</returns>
    public XmlMemberDocumentation? GetTypeDocumentation(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var memberName = GetTypeMemberName(type);
        return _memberDocumentation.TryGetValue(memberName, out var doc) ? doc : null;
    }

    /// <summary>
    /// Gets the documentation for a method.
    /// </summary>
    /// <param name="method">The method to get documentation for.</param>
    /// <returns>The method documentation, or null if not found.</returns>
    public XmlMemberDocumentation? GetMethodDocumentation(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);
        var memberName = GetMethodMemberName(method);
        return _memberDocumentation.TryGetValue(memberName, out var doc) ? doc : null;
    }

    /// <summary>
    /// Gets the documentation for a property.
    /// </summary>
    /// <param name="property">The property to get documentation for.</param>
    /// <returns>The property documentation, or null if not found.</returns>
    public XmlMemberDocumentation? GetPropertyDocumentation(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        var memberName = GetPropertyMemberName(property);
        return _memberDocumentation.TryGetValue(memberName, out var doc) ? doc : null;
    }

    private XmlMemberDocumentation ParseMemberDocumentation(XElement memberElement)
    {
        var documentation = new XmlMemberDocumentation();

        var summaryElement = memberElement.Element("summary");
        if (summaryElement != null)
        {
            documentation.Summary = NormalizeWhitespace(summaryElement.Value);
        }

        var remarksElement = memberElement.Element("remarks");
        if (remarksElement != null)
        {
            documentation.Remarks = NormalizeWhitespace(remarksElement.Value);
        }

        var returnsElement = memberElement.Element("returns");
        if (returnsElement != null)
        {
            documentation.Returns = NormalizeWhitespace(returnsElement.Value);
        }

        // Parse parameter documentation
        var paramElements = memberElement.Elements("param");
        foreach (var paramElement in paramElements)
        {
            var nameAttr = paramElement.Attribute("name");
            if (nameAttr != null)
            {
                documentation.Parameters[nameAttr.Value] = NormalizeWhitespace(paramElement.Value);
            }
        }

        return documentation;
    }

    private string NormalizeWhitespace(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Trim each line and remove empty lines
        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line));

        return string.Join(" ", lines);
    }

    private string GetTypeMemberName(Type type)
    {
        return $"T:{type.FullName?.Replace('+', '.')}";
    }

    private string GetMethodMemberName(MethodInfo method)
    {
        var typeName = method.DeclaringType?.FullName?.Replace('+', '.');
        var methodName = method.Name;

        // Handle parameters
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            return $"M:{typeName}.{methodName}";
        }

        var paramTypes = string.Join(",", parameters.Select(p =>
            p.ParameterType.FullName?.Replace('+', '.')));
        return $"M:{typeName}.{methodName}({paramTypes})";
    }

    private string GetPropertyMemberName(PropertyInfo property)
    {
        var typeName = property.DeclaringType?.FullName?.Replace('+', '.');
        return $"P:{typeName}.{property.Name}";
    }

    /// <summary>
    /// Gets the number of loaded XML documentation files.
    /// </summary>
    public int LoadedFileCount => _loadedFiles.Count;

    /// <summary>
    /// Gets the number of documented members.
    /// </summary>
    public int DocumentedMemberCount => _memberDocumentation.Count;
}

/// <summary>
/// Represents XML documentation for a member.
/// </summary>
public class XmlMemberDocumentation
{
    /// <summary>
    /// Gets or sets the summary text.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the remarks text.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Gets or sets the returns text.
    /// </summary>
    public string? Returns { get; set; }

    /// <summary>
    /// Gets the parameter documentation keyed by parameter name.
    /// </summary>
    public Dictionary<string, string> Parameters { get; } = new();
}
