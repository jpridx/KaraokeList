using Blazored.LocalStorage;

namespace KaraokeList.Web.Services;

public interface ITicklerExclusionsLocalStore
{
    Task<IReadOnlySet<int>> GetExcludedSongIdsAsync();
    Task SaveExcludedSongIdsAsync(IReadOnlyCollection<int> songIds);
    Task AddExcludedSongIdAsync(int songId);
    Task RemoveExcludedSongIdAsync(int songId);
}

public sealed class TicklerExclusionsLocalStore(ILocalStorageService localStorage) : ITicklerExclusionsLocalStore
{
    private const string Key = "karaoke.tickler.exclusions";

    public async Task<IReadOnlySet<int>> GetExcludedSongIdsAsync()
    {
        var stored = await localStorage.GetItemAsync<List<int>?>(Key);
        return stored is null ? new HashSet<int>() : stored.ToHashSet();
    }

    public Task SaveExcludedSongIdsAsync(IReadOnlyCollection<int> songIds) =>
        localStorage.SetItemAsync(Key, songIds.Distinct().ToList()).AsTask();

    public async Task AddExcludedSongIdAsync(int songId)
    {
        var ids = await GetExcludedSongIdsAsync();
        if (ids.Contains(songId))
        {
            return;
        }

        var updated = ids.ToHashSet();
        updated.Add(songId);
        await SaveExcludedSongIdsAsync(updated);
    }

    public async Task RemoveExcludedSongIdAsync(int songId)
    {
        var ids = await GetExcludedSongIdsAsync();
        if (!ids.Contains(songId))
        {
            return;
        }

        var updated = ids.ToHashSet();
        updated.Remove(songId);
        await SaveExcludedSongIdsAsync(updated);
    }
}
