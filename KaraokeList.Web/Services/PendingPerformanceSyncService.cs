using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record PendingSyncResult(int SyncedCount, int RemainingCount, int FailedCount, string? LastError);

public interface IPendingPerformanceSyncService
{
    Task<PendingSyncResult> TrySyncAsync(CancellationToken cancellationToken = default);
}

public sealed class PendingPerformanceSyncService(
    ILogPerformanceLocalStore logStore,
    IKaraokeApiClient api) : IPendingPerformanceSyncService
{
    public async Task<PendingSyncResult> TrySyncAsync(CancellationToken cancellationToken = default)
    {
        var pending = await logStore.GetPendingPerformancesAsync();
        if (pending.Count == 0)
        {
            return new PendingSyncResult(0, 0, 0, null);
        }

        var synced = 0;
        var failed = 0;
        string? lastError = null;

        foreach (var item in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await api.TryCreatePerformanceAsync(item.ToDto());
            if (result.Succeeded)
            {
                await logStore.RemovePendingPerformanceAsync(item.Id);
                synced++;
            }
            else if (result.IsTransient)
            {
                lastError = result.ErrorMessage;
                break;
            }
            else
            {
                await logStore.RemovePendingPerformanceAsync(item.Id);
                failed++;
                lastError = result.ErrorMessage;
            }
        }

        var remaining = await logStore.GetPendingPerformancesAsync();
        return new PendingSyncResult(synced, remaining.Count, failed, lastError);
    }
}
