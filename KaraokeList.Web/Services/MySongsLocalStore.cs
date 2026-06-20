using Blazored.LocalStorage;

namespace KaraokeList.Web.Services;

public interface IMySongsLocalStore
{
    Task<bool> GetShowGenreFiltersAsync();
    Task SetShowGenreFiltersAsync(bool show);
}

public sealed class MySongsLocalStore(ILocalStorageService localStorage) : IMySongsLocalStore
{
    private const string ShowGenreFiltersKey = "karaoke.mySongs.showGenreFilters";

    public async Task<bool> GetShowGenreFiltersAsync()
    {
        var value = await localStorage.GetItemAsync<bool?>(ShowGenreFiltersKey);
        return value ?? false;
    }

    public Task SetShowGenreFiltersAsync(bool show) =>
        localStorage.SetItemAsync(ShowGenreFiltersKey, show).AsTask();
}
