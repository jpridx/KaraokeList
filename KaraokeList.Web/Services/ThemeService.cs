using Blazored.LocalStorage;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace KaraokeList.Web.Services;

public interface IThemeService
{
    Task InitializeAsync();
    Task<ThemePreference> GetPreferenceAsync();
    Task ApplyPreferenceAsync(ThemePreference preference);
    Task<ThemePreferenceUpdateResult> SavePreferenceAsync(ThemePreference preference);
}

public sealed class ThemeService(
    ILocalStorageService localStorage,
    IJSRuntime js,
    IKaraokeApiClient api,
    AuthenticationStateProvider authStateProvider) : IThemeService
{
    public const string StorageKey = "karaoke.theme.preference";

    public async Task InitializeAsync()
    {
        var preference = await GetPreferenceAsync();
        await ApplyPreferenceAsync(preference);
        await js.InvokeVoidAsync("karaokeListTheme.init");
    }

    public async Task<ThemePreference> GetPreferenceAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            var apiResult = await api.GetThemePreferenceAsync();
            if (apiResult.Succeeded && apiResult.Preference is not null)
            {
                return apiResult.Preference.Preference;
            }
        }

        var stored = await localStorage.GetItemAsync<ThemePreference?>(StorageKey);
        return stored ?? ThemePreference.System;
    }

    public async Task ApplyPreferenceAsync(ThemePreference preference)
    {
        await localStorage.SetItemAsync(StorageKey, preference);
        await js.InvokeVoidAsync("karaokeListTheme.setPreference", preference.ToString());
    }

    public async Task<ThemePreferenceUpdateResult> SavePreferenceAsync(ThemePreference preference)
    {
        await ApplyPreferenceAsync(preference);

        var authState = await authStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated != true)
        {
            return ThemePreferenceUpdateResult.Ok();
        }

        return await api.UpdateThemePreferenceAsync(new UpdateThemePreferenceRequest
        {
            Preference = preference
        });
    }
}
