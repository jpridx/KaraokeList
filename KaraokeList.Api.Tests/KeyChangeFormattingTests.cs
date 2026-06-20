using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class KeyChangeFormattingTests
{
    [Theory]
    [InlineData(null, "Original key")]
    [InlineData(0, "Original key")]
    [InlineData(1, "Up 1 half-step")]
    [InlineData(2, "Up 2 half-steps")]
    [InlineData(-1, "Down 1 half-step")]
    [InlineData(-3, "Down 3 half-steps")]
    public void Describe_formats_key_change_labels(int? semitones, string expected)
    {
        Assert.Equal(expected, KeyChangeFormatting.Describe(semitones));
    }
}
