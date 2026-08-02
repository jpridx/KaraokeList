namespace KaraokeList.Shared;

public static class RecentLogsRefreshPolicy
{
    /// <summary>
    /// When true, the Tonight dashboard can show local recent logs without calling my-history.
    /// </summary>
    public static bool ShouldUseLocalRecentLogs(DateTime? newestLoggedAt, DateTime? now = null)
    {
        if (newestLoggedAt is null)
        {
            return false;
        }

        now ??= DateTime.Now;
        return now.Value - newestLoggedAt.Value < CatalogCachePolicy.RefreshThreshold;
    }
}
