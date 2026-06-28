using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class TicklerExclusionIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetSongTicklerExclusion_InitiallyNotExcluded()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var exclusion = await client.GetFromJsonAsync<SongTicklerExclusionDto>(
            $"/api/singers/me/songs/{songId}/tickler-exclusion");

        Assert.NotNull(exclusion);
        Assert.False(exclusion.Excluded);
        Assert.Null(exclusion.Reason);
    }

    [SkippableFact]
    public async Task SetSongTicklerExclusion_ExcludesSongFromStaleSongs()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (staleSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (freshSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, staleSongId, venueId, DateTime.Today.AddDays(-120));
        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, freshSongId, venueId, DateTime.Today.AddDays(-10));

        var set = await client.PutAsJsonAsync(
            $"/api/singers/me/songs/{staleSongId}/tickler-exclusion",
            new UpdateSongTicklerExclusionRequest { Reason = "too hard" });
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var exclusion = await client.GetFromJsonAsync<SongTicklerExclusionDto>(
            $"/api/singers/me/songs/{staleSongId}/tickler-exclusion");
        Assert.NotNull(exclusion);
        Assert.True(exclusion.Excluded);
        Assert.Equal("too hard", exclusion.Reason);

        var response = await client.GetFromJsonAsync<StaleSongsResponseDto>(
            "/api/performances/my-stale-songs?days=90&limit=5");
        Assert.NotNull(response);
        Assert.DoesNotContain(response.Songs, s => s.SongId == staleSongId);
    }

    [SkippableFact]
    public async Task RemoveSongTicklerExclusion_AllowsSongBackInStaleSongs()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (staleSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, staleSongId, venueId, DateTime.Today.AddDays(-120));

        var set = await client.PutAsJsonAsync(
            $"/api/singers/me/songs/{staleSongId}/tickler-exclusion",
            new UpdateSongTicklerExclusionRequest());
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var remove = await client.DeleteAsync(
            $"/api/singers/me/songs/{staleSongId}/tickler-exclusion");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var response = await client.GetFromJsonAsync<StaleSongsResponseDto>(
            "/api/performances/my-stale-songs?days=90&limit=5");
        Assert.NotNull(response);
        Assert.Contains(response.Songs, s => s.SongId == staleSongId);
    }

    [SkippableFact]
    public async Task SetSongTicklerExclusion_RejectsLongReason()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/singers/me/songs/{songId}/tickler-exclusion",
            new UpdateSongTicklerExclusionRequest { Reason = new string('x', 26) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"tickler-exclusion-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
