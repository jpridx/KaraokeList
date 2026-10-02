using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.TestDoubles;

/// <summary>
/// Test loader with a controllable <see cref="LoadAsync"/> queue and real local-store cache/patch behavior.
/// </summary>
public sealed class ControllableMyPerformancesLoader(IMyPerformancesLocalStore store) : IMyPerformancesLoader
{
    private readonly Queue<Func<Task<MyPerformancesLoadResult>>> loadQueue = new();

    public void EnqueueLoad(Func<Task<MyPerformancesLoadResult>> behavior) => loadQueue.Enqueue(behavior);

    public Task<MyPerformancesLoadResult> LoadAsync()
    {
        if (loadQueue.Count == 0)
        {
            throw new InvalidOperationException("No LoadAsync behavior was enqueued.");
        }

        return loadQueue.Dequeue().Invoke();
    }

    public async Task<MyPerformancesLoadResult?> TryGetCachedAsync()
    {
        var cached = await store.GetCachedAsync();
        if (cached is null || cached.Performances.Count == 0)
        {
            return null;
        }

        return new MyPerformancesLoadResult(
            cached.Performances,
            FromCache: true,
            HasCache: true,
            cached.CachedAtUtc,
            null,
            false);
    }

    public async Task PatchPerformanceAsync(MyPerformanceEntryDto updated)
    {
        var cached = await store.GetCachedAsync();
        if (cached is null)
        {
            return;
        }

        var performances = cached.Performances.ToList();
        var index = performances.FindIndex(p => p.Id == updated.Id);
        if (index < 0)
        {
            return;
        }

        performances[index] = updated;
        await store.SaveCachedAsync(new CachedMyPerformances(
            performances,
            DateTime.UtcNow,
            cached.SchemaVersion));
    }

    public Task RemovePerformanceAsync(int performanceId) =>
        throw new NotSupportedException();
}
