namespace SecureSpec.AspNetCore.MediaTypes;

/// <summary>
/// Provides deterministic ordering for media types in OpenAPI content objects.
/// Implements AC 452: Media type ordering matches defined deterministic sequence.
/// </summary>
public static class MediaTypeOrdering
{
    // Deterministic ordering sequence as defined in PRD section 22:
    // application/json → application/xml → text/plain → others lexically →
    // multipart/form-data → application/octet-stream

    private static readonly string[] PriorityOrder =
    [
        "application/json",
        "application/xml",
        "text/plain"
    ];

    private static readonly string[] TrailingOrder =
    [
        "multipart/form-data",
        "application/octet-stream"
    ];

    /// <summary>
    /// Sorts media types according to the deterministic sequence.
    /// </summary>
    /// <param name="mediaTypes">The collection of media types to sort.</param>
    /// <returns>A sorted list of media types.</returns>
    public static IReadOnlyList<string> Sort(IEnumerable<string> mediaTypes)
    {
        ArgumentNullException.ThrowIfNull(mediaTypes);

        var list = mediaTypes.ToList();

        // Sort using the deterministic comparator
        list.Sort(Compare);

        return list;
    }

    /// <summary>
    /// Compares two media types according to the deterministic ordering sequence.
    /// </summary>
    /// <param name="x">First media type.</param>
    /// <param name="y">Second media type.</param>
    /// <returns>
    /// A negative value if x comes before y,
    /// zero if they are equal,
    /// a positive value if x comes after y.
    /// </returns>
    public static int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // Normalize to lowercase for comparison
        // CA1308: Using ToLowerInvariant is appropriate for case-insensitive media type comparison
#pragma warning disable CA1308
        var xNorm = x.ToLowerInvariant();
        var yNorm = y.ToLowerInvariant();
#pragma warning restore CA1308

        int xPriority = GetPriorityIndex(xNorm);
        int yPriority = GetPriorityIndex(yNorm);

        // If both have priority ordering, compare by priority
        if (xPriority >= 0 && yPriority >= 0)
        {
            return xPriority.CompareTo(yPriority);
        }

        // If one has priority and other doesn't
        if (xPriority >= 0) return -1;
        if (yPriority >= 0) return 1;

        // Both are in the middle section - use lexical ordering
        // unless one or both are in trailing section
        int xTrailing = GetTrailingIndex(xNorm);
        int yTrailing = GetTrailingIndex(yNorm);

        // If both in trailing, compare by trailing order
        if (xTrailing >= 0 && yTrailing >= 0)
        {
            return xTrailing.CompareTo(yTrailing);
        }

        // If one is trailing and other is not
        if (xTrailing >= 0) return 1;  // x goes after
        if (yTrailing >= 0) return -1; // y goes after

        // Both are in middle section - lexical ordering
        return string.CompareOrdinal(xNorm, yNorm);
    }

    private static int GetPriorityIndex(string mediaType)
    {
        for (int i = 0; i < PriorityOrder.Length; i++)
        {
            if (mediaType == PriorityOrder[i])
            {
                return i;
            }
        }
        return -1;
    }

    private static int GetTrailingIndex(string mediaType)
    {
        for (int i = 0; i < TrailingOrder.Length; i++)
        {
            if (mediaType == TrailingOrder[i])
            {
                return i;
            }
        }
        return -1;
    }
}
