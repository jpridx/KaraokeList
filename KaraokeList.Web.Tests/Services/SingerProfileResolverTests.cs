using System.Net;
using System.Security.Claims;
using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;

namespace KaraokeList.Web.Tests.Services;

public sealed class SingerProfileResolverTests
{
    [Fact]
    public async Task Returns_claim_singer_id_without_calling_api()
    {
        var api = new Mock<IKaraokeApiClient>();
        var auth = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(KaraokeClaimTypes.SingerId, "7")],
            "test")));

        var singerId = await SingerProfileResolver.ResolveSingerIdAsync(auth, api.Object);

        Assert.Equal(7, singerId);
        api.Verify(client => client.GetProfileAsync(), Times.Never);
    }

    [Fact]
    public async Task Falls_back_to_profile_when_claim_missing()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 12 });

        var auth = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));

        var singerId = await SingerProfileResolver.ResolveSingerIdAsync(auth, api.Object);

        Assert.Equal(12, singerId);
    }

    [Fact]
    public async Task Returns_null_when_profile_unavailable()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetProfileAsync())
            .ThrowsAsync(new HttpRequestException("offline", null, HttpStatusCode.ServiceUnavailable));

        var auth = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));

        var singerId = await SingerProfileResolver.ResolveSingerIdAsync(auth, api.Object);

        Assert.Null(singerId);
    }

    private sealed class TestAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
