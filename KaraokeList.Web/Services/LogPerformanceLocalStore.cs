using Blazored.LocalStorage;

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

public interface ILogPerformanceLocalStore
{
    Task<LogFormDefaults?> GetFormDefaultsAsync();
    Task SaveFormDefaultsAsync(LogFormDefaults defaults);
    Task<IReadOnlyList<RecentLoggedPerformance>> GetRecentLogsAsync();
    Task AddRecentLogAsync(RecentLoggedPerformance entry);
}

public sealed class LogPerformanceLocalStore(ILocalStorageService localStorage) : ILogPerformanceLocalStore
{
    private const string FormDefaultsKey = "karaoke.log.formDefaults";
    private const string RecentLogsKey = "karaoke.log.recentLogs";
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
}
