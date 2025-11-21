namespace SecureSpec.AspNetCore.Core.Attributes;

/// <summary>
/// Marks an endpoint for explicit exclusion from OpenAPI documentation.
/// Takes precedence over all other inclusion logic.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ExcludeFromSpecAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcludeFromSpecAttribute"/> class.
    /// </summary>
    public ExcludeFromSpecAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcludeFromSpecAttribute"/> class with a reason.
    /// </summary>
    /// <param name="reason">Optional reason for exclusion (for diagnostics).</param>
    public ExcludeFromSpecAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason for exclusion (optional, for diagnostics).
    /// </summary>
    public string? Reason { get; }
}
