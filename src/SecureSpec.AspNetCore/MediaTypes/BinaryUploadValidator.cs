using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.MediaTypes;

/// <summary>
/// Validates binary upload requests against configured size thresholds.
/// Implements AC 456: Binary size threshold enforcement logs binaryUploadBlocked before dispatch.
/// </summary>
public class BinaryUploadValidator
{
    private readonly DiagnosticsLogger _logger;
    private readonly long _maxBinarySize;

    /// <summary>
    /// Default maximum binary upload size in bytes (10 MB).
    /// </summary>
    public const long DefaultMaxBinarySize = 10 * 1024 * 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryUploadValidator"/> class.
    /// </summary>
    /// <param name="logger">The diagnostics logger.</param>
    /// <param name="maxBinarySize">Maximum binary upload size in bytes. Default is 10 MB.</param>
    public BinaryUploadValidator(DiagnosticsLogger logger, long maxBinarySize = DefaultMaxBinarySize)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (maxBinarySize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBinarySize), "Max binary size must be greater than zero.");
        }

        _maxBinarySize = maxBinarySize;
    }

    /// <summary>
    /// Gets the configured maximum binary upload size.
    /// </summary>
    public long MaxBinarySize => _maxBinarySize;

    /// <summary>
    /// Validates a binary upload before dispatch.
    /// Logs diagnostic if upload exceeds threshold.
    /// </summary>
    /// <param name="contentLength">The size of the binary content in bytes.</param>
    /// <param name="context">Optional context for diagnostic logging.</param>
    /// <returns>A validation result indicating whether the upload should be allowed.</returns>
    public BinaryUploadValidationResult ValidateBeforeDispatch(long contentLength, object? context = null)
    {
        if (contentLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contentLength), "Content length cannot be negative.");
        }

        if (contentLength > _maxBinarySize)
        {
            var message = $"Binary upload blocked: size {contentLength} bytes exceeds threshold of {_maxBinarySize} bytes.";

            // Log BEFORE dispatch as per AC 456
            _logger.LogError("BIN001", message, new
            {
                ContentLength = contentLength,
                MaxBinarySize = _maxBinarySize,
                Context = context,
                EventType = "binaryUploadBlocked"
            });

            return new BinaryUploadValidationResult
            {
                IsAllowed = false,
                ErrorMessage = message,
                ContentLength = contentLength,
                MaxBinarySize = _maxBinarySize
            };
        }

        return new BinaryUploadValidationResult
        {
            IsAllowed = true,
            ContentLength = contentLength,
            MaxBinarySize = _maxBinarySize
        };
    }

    /// <summary>
    /// Validates a binary upload with additional metadata.
    /// </summary>
    /// <param name="metadata">The binary upload metadata.</param>
    /// <returns>A validation result indicating whether the upload should be allowed.</returns>
    public BinaryUploadValidationResult Validate(BinaryUploadMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return ValidateBeforeDispatch(metadata.ContentLength, new
        {
            metadata.ContentType,
            metadata.FileName
        });
    }
}

/// <summary>
/// Represents the result of binary upload validation.
/// </summary>
public class BinaryUploadValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the upload is allowed.
    /// </summary>
    public required bool IsAllowed { get; init; }

    /// <summary>
    /// Gets or sets the error message if upload was blocked.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the content length in bytes.
    /// </summary>
    public required long ContentLength { get; init; }

    /// <summary>
    /// Gets or sets the maximum allowed binary size.
    /// </summary>
    public required long MaxBinarySize { get; init; }
}

/// <summary>
/// Represents metadata for a binary upload.
/// </summary>
public class BinaryUploadMetadata
{
    /// <summary>
    /// Gets or sets the content length in bytes.
    /// </summary>
    public required long ContentLength { get; init; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the file name if applicable.
    /// </summary>
    public string? FileName { get; init; }
}
