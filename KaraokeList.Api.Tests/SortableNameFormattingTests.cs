using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class SortableNameFormattingTests
{
    [Theory]
    [InlineData("The Birds", "Birds, The")]
    [InlineData("the birds", "birds, the")]
    [InlineData("A Flock of Seagulls", "Flock of Seagulls, A")]
    [InlineData("An Evening with", "Evening with, An")]
    [InlineData("Slayer", null)]
    [InlineData("The", null)]
    [InlineData("  The Beatles  ", "Beatles, The")]
    public void FromDisplayName_moves_leading_articles_to_end(string name, string? expected)
    {
        Assert.Equal(expected, SortableNameFormatting.FromDisplayName(name));
    }

    [Fact]
    public void FromDisplayName_preserves_explicit_sortable_name_when_set_separately()
    {
        Assert.Null(SortableNameFormatting.FromDisplayName("Slayer"));
    }
}
