namespace SecureSpec.AspNetCore.Core.Attributes;

/// <summary>
/// Marks an endpoint for explicit inclusion in OpenAPI documentation.
/// Takes precedence over conventions and exclusions.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class IncludeInSpecAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeInSpecAttribute"/> class.
    /// </summary>
    public IncludeInSpecAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeInSpecAttribute"/> class with a document filter.
    /// </summary>
    /// <param name="documentNames">Optional list of document names to include this endpoint in.</param>
    public IncludeInSpecAttribute(params string[] documentNames)
    {
        DocumentNames = documentNames;
    }

    /// <summary>
    /// Gets the specific document names this endpoint should be included in.
    /// If null or empty, endpoint is included in all documents.
    /// </summary>
    public string[]? DocumentNames { get; }
}
