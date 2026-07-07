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
        var store = new SingerProfileLocalStore(new InMemoryLocalStorage());
        var auth = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(KaraokeClaimTypes.SingerId, "7")],
            "test")));
        var resolver = new SingerProfileResolver(auth, api.Object, store);

        var result = await resolver.ResolveAsync();

        Assert.Equal(7, result.SingerId);
        Assert.False(result.UsedCachedSingerId);
        api.Verify(client => client.GetProfileAsync(), Times.Never);
        Assert.Equal(7, await store.GetCachedSingerIdAsync());
    }

    [Fact]
    public async Task Uses_cached_singer_id_before_calling_api()
    {
        var api = new Mock<IKaraokeApiClient>();
        var store = new SingerProfileLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedSingerIdAsync(12);
        var auth = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));
        var resolver = new SingerProfileResolver(auth, api.Object, store);

        var result = await resolver.ResolveAsync();

        Assert.Equal(12, result.SingerId);
        Assert.True(result.UsedCachedSingerId);
        api.Verify(client => client.GetProfileAsync(), Times.Never);
    }

    [Fact]
    public async Task Falls_back_to_profile_when_claim_and_cache_missing()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 12 });
        var store = new SingerProfileLocalStore(new InMemoryLocalStorage());
        var auth = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));
        var resolver = new SingerProfileResolver(auth, api.Object, store);

        var result = await resolver.ResolveAsync();

        Assert.Equal(12, result.SingerId);
        Assert.False(result.UsedCachedSingerId);
        Assert.Equal(12, await store.GetCachedSingerIdAsync());
    }

    [Fact]
    public async Task Returns_null_when_profile_unavailable()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetProfileAsync())
            .ThrowsAsync(new HttpRequestException("offline", null, HttpStatusCode.ServiceUnavailable));
        var store = new SingerProfileLocalStore(new InMemoryLocalStorage());
        var auth = new TestAuthStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));
        var resolver = new SingerProfileResolver(auth, api.Object, store);

        var result = await resolver.ResolveAsync();

        Assert.Null(result.SingerId);
        Assert.False(result.UsedCachedSingerId);
    }

    private sealed class TestAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(user));
    }
}
