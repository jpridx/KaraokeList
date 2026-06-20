using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class ShowHostMessageFormattingTests
{
    [Theory]
    [InlineData("Footloose", "Kenny Loggins", null, "Footloose - Kenny Loggins")]
    [InlineData("Footloose", "Kenny Loggins", 0, "Footloose - Kenny Loggins")]
    [InlineData("Footloose", "Kenny Loggins", 2, "Footloose - Kenny Loggins (Up 2)")]
    [InlineData("Footloose", "Kenny Loggins", -1, "Footloose - Kenny Loggins (Down 1)")]
    [InlineData("  Footloose  ", "  Kenny Loggins  ", null, "Footloose - Kenny Loggins")]
    [InlineData("Footloose", "", null, "Footloose")]
    [InlineData("Footloose", "   ", null, "Footloose")]
    public void Format_builds_title_artist_and_optional_key_suffix(
        string title,
        string artistName,
        int? keyChangeSemitones,
        string expected)
    {
        Assert.Equal(expected, ShowHostMessageFormatting.Format(title, artistName, keyChangeSemitones));
    }
}
