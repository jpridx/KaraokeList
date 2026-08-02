using Blazored.LocalStorage;
using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface ITicklerSettingsLocalStore
{
    Task<TicklerSettingsDto> GetAsync();
    Task SaveAsync(TicklerSettingsDto settings);
}

public sealed class TicklerSettingsLocalStore(ILocalStorageService localStorage) : ITicklerSettingsLocalStore
{
    private const string Key = "karaoke.tickler.settings";

    public async Task<TicklerSettingsDto> GetAsync()
    {
        var stored = await localStorage.GetItemAsync<TicklerSettingsDto?>(Key);
        return stored ?? new TicklerSettingsDto();
    }

    public Task SaveAsync(TicklerSettingsDto settings) =>
        localStorage.SetItemAsync(Key, settings).AsTask();
}
