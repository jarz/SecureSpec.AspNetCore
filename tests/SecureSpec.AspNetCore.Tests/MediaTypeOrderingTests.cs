using SecureSpec.AspNetCore.MediaTypes;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for AC 452: Media type ordering matches defined deterministic sequence.
/// </summary>
public class MediaTypeOrderingTests
{
    [Fact]
    public void Sort_AppliesDeterministicOrdering()
    {
        // Arrange - unsorted list
        var mediaTypes = new[]
        {
            "application/octet-stream",
            "text/plain",
            "application/json",
            "multipart/form-data",
            "application/xml",
            "application/custom"
        };

        // Act
        var sorted = MediaTypeOrdering.Sort(mediaTypes);

        // Assert - expected order:
        // application/json → application/xml → text/plain → others lexically →
        // multipart/form-data → application/octet-stream
        Assert.Equal(6, sorted.Count);
        Assert.Equal("application/json", sorted[0]);
        Assert.Equal("application/xml", sorted[1]);
        Assert.Equal("text/plain", sorted[2]);
        Assert.Equal("application/custom", sorted[3]); // Lexical position
        Assert.Equal("multipart/form-data", sorted[4]);
        Assert.Equal("application/octet-stream", sorted[5]);
    }

    [Fact]
    public void Sort_HandlesCaseInsensitivity()
    {
        // Arrange
        var mediaTypes = new[]
        {
            "APPLICATION/JSON",
            "Text/Plain",
            "application/XML"
        };

        // Act
        var sorted = MediaTypeOrdering.Sort(mediaTypes);

        // Assert
        Assert.Equal("APPLICATION/JSON", sorted[0]);
        Assert.Equal("application/XML", sorted[1]);
        Assert.Equal("Text/Plain", sorted[2]);
    }

    [Fact]
    public void Sort_StableLexicalOrderingForUnknownTypes()
    {
        // Arrange
        var mediaTypes = new[]
        {
            "application/zebra",
            "application/alpha",
            "application/beta"
        };

        // Act
        var sorted = MediaTypeOrdering.Sort(mediaTypes);

        // Assert - lexical ordering
        Assert.Equal("application/alpha", sorted[0]);
        Assert.Equal("application/beta", sorted[1]);
        Assert.Equal("application/zebra", sorted[2]);
    }

    [Fact]
    public void Compare_ReturnsZeroForEqualMediaTypes()
    {
        // Act & Assert
        Assert.Equal(0, MediaTypeOrdering.Compare("application/json", "application/json"));
        Assert.Equal(0, MediaTypeOrdering.Compare(null, null));
    }

    [Fact]
    public void Compare_HandlesNullValues()
    {
        // Act & Assert
        Assert.True(MediaTypeOrdering.Compare(null, "application/json") < 0);
        Assert.True(MediaTypeOrdering.Compare("application/json", null) > 0);
    }

    [Fact]
    public void Compare_PrioritizesPriorityMediaTypes()
    {
        // Assert - application/json before application/xml
        Assert.True(MediaTypeOrdering.Compare("application/json", "application/xml") < 0);

        // Assert - application/xml before text/plain
        Assert.True(MediaTypeOrdering.Compare("application/xml", "text/plain") < 0);

        // Assert - text/plain before unknown types
        Assert.True(MediaTypeOrdering.Compare("text/plain", "application/custom") < 0);
    }

    [Fact]
    public void Compare_PrioritizesTrailingMediaTypesLast()
    {
        // Assert - unknown types before multipart/form-data
        Assert.True(MediaTypeOrdering.Compare("application/custom", "multipart/form-data") < 0);

        // Assert - multipart/form-data before application/octet-stream
        Assert.True(MediaTypeOrdering.Compare("multipart/form-data", "application/octet-stream") < 0);

        // Assert - priority types before trailing types
        Assert.True(MediaTypeOrdering.Compare("application/json", "application/octet-stream") < 0);
    }

    [Fact]
    public void Sort_PreservesStableOrderingAcrossInvocations()
    {
        // Arrange
        var mediaTypes = new[]
        {
            "application/octet-stream",
            "text/plain",
            "application/json",
            "application/custom",
            "multipart/form-data",
            "application/xml"
        };

        // Act - sort multiple times
        var sorted1 = MediaTypeOrdering.Sort(mediaTypes);
        var sorted2 = MediaTypeOrdering.Sort(mediaTypes);

        // Assert - same order both times
        Assert.Equal(sorted1, sorted2);
    }

    [Fact]
    public void Sort_EmptyCollectionReturnsEmptyList()
    {
        // Arrange
        var mediaTypes = Array.Empty<string>();

        // Act
        var sorted = MediaTypeOrdering.Sort(mediaTypes);

        // Assert
        Assert.Empty(sorted);
    }

    [Fact]
    public void Sort_ThrowsOnNullInput()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => MediaTypeOrdering.Sort(null!));
    }
}
