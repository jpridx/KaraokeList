using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.TestDoubles;

public sealed class NoOpPerformanceCacheCoordinator : IPerformanceCacheCoordinator
{
    public event Action? RecentLogsChanged;

    public event Action? RepertoireStatsChanged;

    public Task PatchAfterUpdateAsync(PerformanceEditSnapshot before, PerformanceEditSnapshot after) =>
        Task.CompletedTask;

    public Task PatchAfterDeleteAsync(PerformanceEditSnapshot deleted) =>
        Task.CompletedTask;

    public Task RebuildRecentLogsFromPerformancesAsync() =>
        Task.CompletedTask;
}
