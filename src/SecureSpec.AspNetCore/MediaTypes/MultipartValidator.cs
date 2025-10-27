using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.MediaTypes;

/// <summary>
/// Validates multipart form data requests against configured limits.
/// Implements AC 454: Multipart validator enforces field count limit (diagnostic BND001).
/// Implements AC 455: Multipart file + field mix preserves ordering and validation messages.
/// </summary>
public class MultipartValidator
{
    private readonly DiagnosticsLogger _logger;
    private readonly int _maxFieldCount;

    /// <summary>
    /// Default maximum field count for multipart requests.
    /// </summary>
    public const int DefaultMaxFieldCount = 200;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultipartValidator"/> class.
    /// </summary>
    /// <param name="logger">The diagnostics logger.</param>
    /// <param name="maxFieldCount">Maximum number of fields (names + files) allowed. Default is 200.</param>
    public MultipartValidator(DiagnosticsLogger logger, int maxFieldCount = DefaultMaxFieldCount)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (maxFieldCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFieldCount), "Max field count must be greater than zero.");
        }

        _maxFieldCount = maxFieldCount;
    }

    /// <summary>
    /// Gets the configured maximum field count.
    /// </summary>
    public int MaxFieldCount => _maxFieldCount;

    /// <summary>
    /// Validates a multipart request and returns validation result.
    /// </summary>
    /// <param name="fieldCount">The total number of fields in the multipart request (names + files).</param>
    /// <returns>A validation result indicating whether the request is valid.</returns>
    public MultipartValidationResult Validate(int fieldCount)
    {
        if (fieldCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldCount), "Field count cannot be negative.");
        }

        if (fieldCount > _maxFieldCount)
        {
            var message = $"Multipart request exceeded field count limit. Received {fieldCount} fields, maximum allowed is {_maxFieldCount}.";

            _logger.LogError("BND001", message, new
            {
                FieldCount = fieldCount,
                MaxFieldCount = _maxFieldCount
            });

            return new MultipartValidationResult
            {
                IsValid = false,
                ErrorMessage = message,
                FieldCount = fieldCount,
                MaxFieldCount = _maxFieldCount
            };
        }

        return new MultipartValidationResult
        {
            IsValid = true,
            FieldCount = fieldCount,
            MaxFieldCount = _maxFieldCount
        };
    }

    /// <summary>
    /// Validates a collection of multipart fields (preserving ordering).
    /// </summary>
    /// <param name="fields">The ordered collection of field metadata.</param>
    /// <returns>A validation result with detailed field information.</returns>
    public MultipartValidationResult ValidateFields(IReadOnlyList<MultipartField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var result = Validate(fields.Count);

        if (!result.IsValid)
        {
            // Preserve field ordering and types in validation messages
            result.Fields = fields;
        }

        return result;
    }
}

/// <summary>
/// Represents the result of multipart validation.
/// </summary>
public class MultipartValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the number of fields in the request.
    /// </summary>
    public required int FieldCount { get; init; }

    /// <summary>
    /// Gets or sets the maximum allowed field count.
    /// </summary>
    public required int MaxFieldCount { get; init; }

    /// <summary>
    /// Gets or sets the ordered list of fields (preserved for validation messages).
    /// </summary>
    public IReadOnlyList<MultipartField>? Fields { get; set; }
}

/// <summary>
/// Represents metadata for a multipart field.
/// </summary>
public class MultipartField
{
    /// <summary>
    /// Gets or sets the field name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this field is a file upload.
    /// </summary>
    public required bool IsFile { get; init; }

    /// <summary>
    /// Gets or sets the content type of the field (for file uploads).
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the size of the field value in bytes.
    /// </summary>
    public long? Size { get; init; }
}
