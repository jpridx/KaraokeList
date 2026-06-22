namespace KaraokeList.Shared;

public static class LogKeyChangeDefaults
{
    /// <summary>
    /// Key picker default when opening Log for a song.
    /// Uses the last key for that song when you've performed it before; otherwise original key.
    /// When the summary is unavailable (offline), falls back to a recent on-device log for the same song.
    /// </summary>
    public static int? ForSong(SongPerformanceSummaryDto? summary, int? recentLogKeyForSameSong = null)
    {
        if (summary is { PerformanceCount: > 0 })
        {
            return summary.LastKeyChangeSemitones;
        }

        if (summary is null && recentLogKeyForSameSong is not null)
        {
            return recentLogKeyForSameSong;
        }

        return null;
    }
}
