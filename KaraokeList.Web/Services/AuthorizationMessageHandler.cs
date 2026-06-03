using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace KaraokeList.Web.Services;

public sealed class AuthorizationMessageHandler(ILocalStorageService localStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await localStorage.GetItemAsStringAsync(JwtAuthenticationStateProvider.TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim('"'));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
