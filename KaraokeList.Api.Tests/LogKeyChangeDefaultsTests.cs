using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class LogKeyChangeDefaultsTests
{
    [Fact]
    public void ForSong_never_performed_returns_original_key()
    {
        var summary = new SongPerformanceSummaryDto { SongId = 1, PerformanceCount = 0 };

        Assert.Null(LogKeyChangeDefaults.ForSong(summary));
    }

    [Fact]
    public void ForSong_no_summary_and_no_recent_log_returns_original_key()
    {
        Assert.Null(LogKeyChangeDefaults.ForSong(null));
    }

    [Fact]
    public void ForSong_performed_before_uses_last_key()
    {
        var summary = new SongPerformanceSummaryDto
        {
            SongId = 1,
            PerformanceCount = 3,
            LastKeyChangeSemitones = -2
        };

        Assert.Equal(-2, LogKeyChangeDefaults.ForSong(summary));
    }

    [Fact]
    public void ForSong_performed_before_with_original_key_returns_null()
    {
        var summary = new SongPerformanceSummaryDto
        {
            SongId = 1,
            PerformanceCount = 1,
            LastKeyChangeSemitones = null
        };

        Assert.Null(LogKeyChangeDefaults.ForSong(summary));
    }

    [Fact]
    public void ForSong_ignores_stored_key_from_other_songs()
    {
        var summary = new SongPerformanceSummaryDto { SongId = 1, PerformanceCount = 0 };

        Assert.Null(LogKeyChangeDefaults.ForSong(summary, recentLogKeyForSameSong: 2));
    }

    [Fact]
    public void ForSong_offline_uses_recent_log_for_same_song()
    {
        Assert.Equal(-1, LogKeyChangeDefaults.ForSong(null, recentLogKeyForSameSong: -1));
    }
}
