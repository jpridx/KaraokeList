using System.Net.Http;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Components.Authorization;

namespace KaraokeList.Web.Services;

public sealed record SingerProfileResolveResult(int? SingerId, bool UsedCachedSingerId);

public interface ISingerProfileResolver
{
    Task<SingerProfileResolveResult> ResolveAsync();
    Task RefreshFromApiAsync();
}

public sealed class SingerProfileResolver(
    AuthenticationStateProvider authStateProvider,
    IKaraokeApiClient api,
    ISingerProfileLocalStore store) : ISingerProfileResolver
{
    public async Task<SingerProfileResolveResult> ResolveAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var claimSingerId = authState.User.GetSingerId();
        if (claimSingerId is int fromClaim)
        {
            await store.SaveCachedSingerIdAsync(fromClaim);
            return new SingerProfileResolveResult(fromClaim, false);
        }

        var cachedSingerId = await store.GetCachedSingerIdAsync();
        if (cachedSingerId is int cachedId)
        {
            return new SingerProfileResolveResult(cachedId, true);
        }

        var profileSingerId = await TryFetchSingerIdFromApiAsync();
        if (profileSingerId is int profileId)
        {
            await store.SaveCachedSingerIdAsync(profileId);
            return new SingerProfileResolveResult(profileId, false);
        }

        return new SingerProfileResolveResult(null, false);
    }

    public async Task RefreshFromApiAsync()
    {
        var profileSingerId = await TryFetchSingerIdFromApiAsync();
        if (profileSingerId is int profileId)
        {
            await store.SaveCachedSingerIdAsync(profileId);
        }
    }

    private async Task<int?> TryFetchSingerIdFromApiAsync()
    {
        try
        {
            var profileTask = api.GetProfileAsync();
            if (await Task.WhenAny(profileTask, Task.Delay(ApiSlowRequestNotifier.PageLoadTimeout))
                != profileTask)
            {
                return null;
            }

            return (await profileTask)?.SingerId;
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException)
        {
            return null;
        }
    }
}
