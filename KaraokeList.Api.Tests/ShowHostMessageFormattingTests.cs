using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class ShowHostMessageFormattingTests
{
    [Theory]
    [InlineData("Footloose", "Kenny Loggins", null, null, "Footloose - Kenny Loggins")]
    [InlineData("Footloose", "Kenny Loggins", 0, null, "Footloose - Kenny Loggins")]
    [InlineData("Footloose", "Kenny Loggins", 2, null, "Footloose - Kenny Loggins (Up 2)")]
    [InlineData("Footloose", "Kenny Loggins", -1, null, "Footloose - Kenny Loggins (Down 1)")]
    [InlineData("  Footloose  ", "  Kenny Loggins  ", null, null, "Footloose - Kenny Loggins")]
    [InlineData("Footloose", "", null, null, "Footloose")]
    [InlineData("Footloose", "   ", null, null, "Footloose")]
    [InlineData("Islands in the Stream", "Kenny Rogers", null, new[] { "Dolly Parton" }, "Islands in the Stream - Kenny Rogers (with Dolly Parton)")]
    [InlineData("Shallow", "Lady Gaga", 1, new[] { "Bradley Cooper" }, "Shallow - Lady Gaga (with Bradley Cooper) (Up 1)")]
    public void Format_builds_title_artist_and_optional_key_suffix(
        string title,
        string artistName,
        int? keyChangeSemitones,
        string[]? coPerformers,
        string expected)
    {
        Assert.Equal(expected, ShowHostMessageFormatting.Format(title, artistName, keyChangeSemitones, coPerformers));
    }

    [Fact]
    public void Format_without_coPerformers_matches_original_signature()
    {
        Assert.Equal(
            "Footloose - Kenny Loggins (Up 2)",
            ShowHostMessageFormatting.Format("Footloose", "Kenny Loggins", 2));
    }
}
