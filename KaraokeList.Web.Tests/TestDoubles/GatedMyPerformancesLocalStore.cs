using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.TestDoubles;

/// <summary>
/// Wraps a performances store and can pause the next <see cref="SaveCachedAsync"/> until signaled.
/// </summary>
public sealed class GatedMyPerformancesLocalStore(IMyPerformancesLocalStore inner) : IMyPerformancesLocalStore
{
    private readonly object gate = new();
    private TaskCompletionSource? pauseSaveUntil;

    public void PauseNextSaveUntil(TaskCompletionSource releaseSave)
    {
        lock (gate)
        {
            pauseSaveUntil = releaseSave;
        }
    }

    public Task<CachedMyPerformances?> GetCachedAsync() => inner.GetCachedAsync();

    public async Task SaveCachedAsync(CachedMyPerformances cache)
    {
        TaskCompletionSource? pause;
        lock (gate)
        {
            pause = pauseSaveUntil;
            pauseSaveUntil = null;
        }

        if (pause is not null)
        {
            await pause.Task;
        }

        await inner.SaveCachedAsync(cache);
    }

    public Task ClearCacheAsync() => inner.ClearCacheAsync();
}
