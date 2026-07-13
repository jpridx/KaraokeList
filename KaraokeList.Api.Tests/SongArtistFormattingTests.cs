using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class SongArtistFormattingTests
{
    [Fact]
    public void FormatDisplay_prefers_credit_display()
    {
        var result = SongArtistFormatting.FormatDisplay("Lady Gaga feat. Beyoncé", ["Lady Gaga", "Beyoncé"]);
        Assert.Equal("Lady Gaga feat. Beyoncé", result);
    }

    [Fact]
    public void FormatDisplay_joins_names_when_credit_display_missing()
    {
        var result = SongArtistFormatting.FormatDisplay(null, ["Lady Gaga", "Beyoncé"]);
        Assert.Equal("Lady Gaga, Beyoncé", result);
    }
}
