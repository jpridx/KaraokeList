using Blazored.LocalStorage;
using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record LogFormDefaults(int? VenueId, DateTime PerformedOn, int? KeyChangeSemitones);

public sealed record RecentLoggedPerformance(
    int SongId,
    string Title,
    string ArtistName,
    string VenueName,
    DateTime PerformedOn,
    int? KeyChangeSemitones,
    DateTime LoggedAt);

public sealed record PendingPerformanceEntry(
    Guid Id,
    int SingerId,
    int SongId,
    int VenueId,
    DateTime PerformedOn,
    int? KeyChangeSemitones,
    string Title,
    string ArtistName,
    string VenueName,
    DateTime QueuedAt)
{
    public PerformanceDto ToDto() => new()
    {
        Singer = SingerId,
        Song = SongId,
        Venue = VenueId,
        PerformedOn = PerformedOn,
        KeyChangeSemitones = KeyChangeSemitones
    };
}

public interface ILogPerformanceLocalStore
{
    Task<LogFormDefaults?> GetFormDefaultsAsync();
    Task SaveFormDefaultsAsync(LogFormDefaults defaults);
    Task<IReadOnlyList<RecentLoggedPerformance>> GetRecentLogsAsync();
    Task AddRecentLogAsync(RecentLoggedPerformance entry);
    Task<IReadOnlyList<PendingPerformanceEntry>> GetPendingPerformancesAsync();
    Task EnqueuePendingPerformanceAsync(PendingPerformanceEntry entry);
    Task RemovePendingPerformanceAsync(Guid id);
    Task<CachedLogCatalog?> GetCachedCatalogAsync();
    Task SaveCachedCatalogAsync(CachedLogCatalog catalog);
}

public sealed class LogPerformanceLocalStore(ILocalStorageService localStorage) : ILogPerformanceLocalStore
{
    private const string FormDefaultsKey = "karaoke.log.formDefaults";
    private const string RecentLogsKey = "karaoke.log.recentLogs";
    private const string PendingPerformancesKey = "karaoke.log.pendingPerformances";
    private const string CachedCatalogKey = "karaoke.log.cachedCatalog";
    private const int MaxRecentLogs = 5;

    public Task<LogFormDefaults?> GetFormDefaultsAsync() =>
        localStorage.GetItemAsync<LogFormDefaults?>(FormDefaultsKey).AsTask();

    public Task SaveFormDefaultsAsync(LogFormDefaults defaults) =>
        localStorage.SetItemAsync(FormDefaultsKey, defaults).AsTask();

    public async Task<IReadOnlyList<RecentLoggedPerformance>> GetRecentLogsAsync()
    {
        var logs = await localStorage.GetItemAsync<List<RecentLoggedPerformance>>(RecentLogsKey);
        return logs ?? [];
    }

    public async Task AddRecentLogAsync(RecentLoggedPerformance entry)
    {
        var logs = await localStorage.GetItemAsync<List<RecentLoggedPerformance>>(RecentLogsKey) ?? [];
        logs.Insert(0, entry);
        if (logs.Count > MaxRecentLogs)
        {
            logs = logs.Take(MaxRecentLogs).ToList();
        }

        await localStorage.SetItemAsync(RecentLogsKey, logs);
    }

    public async Task<IReadOnlyList<PendingPerformanceEntry>> GetPendingPerformancesAsync()
    {
        var pending = await localStorage.GetItemAsync<List<PendingPerformanceEntry>>(PendingPerformancesKey);
        return pending ?? [];
    }

    public async Task EnqueuePendingPerformanceAsync(PendingPerformanceEntry entry)
    {
        var pending = await localStorage.GetItemAsync<List<PendingPerformanceEntry>>(PendingPerformancesKey) ?? [];
        pending.Add(entry);
        await localStorage.SetItemAsync(PendingPerformancesKey, pending);
    }

    public async Task RemovePendingPerformanceAsync(Guid id)
    {
        var pending = await localStorage.GetItemAsync<List<PendingPerformanceEntry>>(PendingPerformancesKey) ?? [];
        var removed = pending.RemoveAll(p => p.Id == id);
        if (removed == 0)
        {
            return;
        }

        await localStorage.SetItemAsync(PendingPerformancesKey, pending);
    }

    public Task<CachedLogCatalog?> GetCachedCatalogAsync() =>
        localStorage.GetItemAsync<CachedLogCatalog?>(CachedCatalogKey).AsTask();

    public Task SaveCachedCatalogAsync(CachedLogCatalog catalog) =>
        localStorage.SetItemAsync(CachedCatalogKey, catalog).AsTask();
}
