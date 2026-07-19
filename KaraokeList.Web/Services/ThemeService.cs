using System.Text.Json;
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

        return await TryGetStoredPreferenceAsync();
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

    private async Task<ThemePreference> TryGetStoredPreferenceAsync()
    {
        try
        {
            var stored = await localStorage.GetItemAsync<ThemePreference?>(StorageKey);
            if (stored is not null)
            {
                return stored.Value;
            }
        }
        catch (JsonException)
        {
            // Legacy plain-string values (e.g. "Dark") are not valid JSON for Blazored.
        }

        var raw = await localStorage.GetItemAsStringAsync(StorageKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ThemePreference.System;
        }

        var trimmed = raw.Trim().Trim('"');
        if (TryParseLegacyPreference(trimmed, out var legacy))
        {
            await localStorage.SetItemAsync(StorageKey, legacy);
            return legacy;
        }

        await localStorage.RemoveItemAsync(StorageKey);
        return ThemePreference.System;
    }

    internal static bool TryParseLegacyPreference(string raw, out ThemePreference preference)
    {
        preference = ThemePreference.System;

        if (raw is "0" or "1" or "2")
        {
            preference = (ThemePreference)int.Parse(raw);
            return true;
        }

        if (Enum.TryParse(raw, ignoreCase: true, out ThemePreference parsed)
            && ThemePreferenceCatalog.SelectablePreferences.Contains(parsed))
        {
            preference = parsed;
            return true;
        }

        return false;
    }
}
