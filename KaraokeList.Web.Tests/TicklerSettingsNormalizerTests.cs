using KaraokeList.Shared;

namespace KaraokeList.Web.Tests;

public sealed class TicklerSettingsNormalizerTests
{
    [Fact]
    public void Normalize_applies_defaults_for_zero_song_limit()
    {
        var normalized = TicklerSettingsNormalizer.Normalize(new TicklerSettingsDto
        {
            StaleAfterDays = 45,
            SongLimit = 0
        });

        Assert.Equal(45, normalized.StaleAfterDays);
        Assert.Equal(TicklerSettingsLimits.DefaultSongLimit, normalized.SongLimit);
    }

    [Fact]
    public void Normalize_applies_defaults_for_zero_stale_after_days()
    {
        var normalized = TicklerSettingsNormalizer.Normalize(new TicklerSettingsDto
        {
            StaleAfterDays = 0,
            SongLimit = 3
        });

        Assert.Equal(TicklerSettingsLimits.DefaultStaleAfterDays, normalized.StaleAfterDays);
        Assert.Equal(3, normalized.SongLimit);
    }

    [Fact]
    public void Normalize_applies_defaults_for_null_settings()
    {
        var normalized = TicklerSettingsNormalizer.Normalize(null);

        Assert.Equal(TicklerSettingsLimits.DefaultStaleAfterDays, normalized.StaleAfterDays);
        Assert.Equal(TicklerSettingsLimits.DefaultSongLimit, normalized.SongLimit);
    }

    [Fact]
    public void Normalize_preserves_valid_values()
    {
        var normalized = TicklerSettingsNormalizer.Normalize(new TicklerSettingsDto
        {
            StaleAfterDays = 120,
            SongLimit = 8
        });

        Assert.Equal(120, normalized.StaleAfterDays);
        Assert.Equal(8, normalized.SongLimit);
    }
}
