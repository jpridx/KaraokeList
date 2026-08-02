using KaraokeList.Shared;

namespace KaraokeList.Web.Tests.Services;

public sealed class RecentLogsRefreshPolicyTests
{
    [Fact]
    public void ShouldUseLocalRecentLogs_false_when_no_logs()
    {
        Assert.False(RecentLogsRefreshPolicy.ShouldUseLocalRecentLogs(null, new DateTime(2026, 8, 2, 21, 0, 0)));
    }

    [Fact]
    public void ShouldUseLocalRecentLogs_true_when_logged_within_threshold()
    {
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var loggedAt = now.AddHours(-1);

        Assert.True(RecentLogsRefreshPolicy.ShouldUseLocalRecentLogs(loggedAt, now));
    }

    [Fact]
    public void ShouldUseLocalRecentLogs_false_when_logged_before_threshold()
    {
        var now = new DateTime(2026, 8, 2, 21, 0, 0);
        var loggedAt = now - CatalogCachePolicy.RefreshThreshold;

        Assert.False(RecentLogsRefreshPolicy.ShouldUseLocalRecentLogs(loggedAt, now));
    }
}
