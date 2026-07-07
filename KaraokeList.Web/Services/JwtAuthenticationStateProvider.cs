using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace KaraokeList.Web.Services;

public sealed class JwtAuthenticationStateProvider(
    ILocalStorageService localStorage,
    ISingerProfileLocalStore singerProfileStore) : AuthenticationStateProvider
{
    public const string TokenKey = "authToken";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsStringAsync(TokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token.Trim('"'));
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await localStorage.RemoveItemAsync(TokenKey);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt");
            var principal = new ClaimsPrincipal(identity);
            if (principal.GetSingerId() is int singerId)
            {
                await singerProfileStore.SaveCachedSingerIdAsync(singerId);
            }

            return new AuthenticationState(principal);
        }
        catch
        {
            await localStorage.RemoveItemAsync(TokenKey);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await localStorage.SetItemAsStringAsync(TokenKey, token);
        await CacheSingerIdFromTokenAsync(token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await localStorage.RemoveItemAsync(TokenKey);
        await singerProfileStore.ClearCachedSingerIdAsync();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task CacheSingerIdFromTokenAsync(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Trim('"'));
            var singerIdValue = jwt.Claims.FirstOrDefault(c => c.Type == KaraokeClaimTypes.SingerId)?.Value;
            if (int.TryParse(singerIdValue, out var singerId))
            {
                await singerProfileStore.SaveCachedSingerIdAsync(singerId);
            }
        }
        catch
        {
            // Non-fatal; profile resolver can refresh later.
        }
    }
}
