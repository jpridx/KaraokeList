using System.Net.Http;
using Microsoft.AspNetCore.Components.Authorization;

namespace KaraokeList.Web.Services;

public static class SingerProfileResolver
{
    public static async Task<int?> ResolveSingerIdAsync(
        AuthenticationStateProvider authStateProvider,
        IKaraokeApiClient api)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var singerId = authState.User.GetSingerId();
        if (singerId is not null)
        {
            return singerId;
        }

        try
        {
            var profile = await api.GetProfileAsync();
            return profile?.SingerId;
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException)
        {
            return null;
        }
    }
}
