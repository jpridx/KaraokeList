using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class StaleSongsIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetMyStaleSongs_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/performances/my-stale-songs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetMyStaleSongs_ReturnsSongsNotPerformedRecently()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (staleSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (freshSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, staleSongId, venueId, DateTime.Today.AddDays(-120));
        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, freshSongId, venueId, DateTime.Today.AddDays(-10));

        var response = await client.GetFromJsonAsync<StaleSongsResponseDto>(
            "/api/performances/my-stale-songs?days=90&limit=5");

        Assert.NotNull(response);
        Assert.Equal(90, response.StaleAfterDays);
        Assert.Contains(response.Songs, s => s.SongId == staleSongId);
        Assert.DoesNotContain(response.Songs, s => s.SongId == freshSongId);
        var stale = Assert.Single(response.Songs, s => s.SongId == staleSongId);
        Assert.True(stale.DaysSinceLastPerformed >= 90);
        Assert.Equal(1, stale.PerformanceCount);
    }

    [SkippableFact]
    public async Task GetMyStaleSongs_IncludesUnperformedRepertoireSongs()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await client.GetFromJsonAsync<List<SingerListDto>>("/api/singers/me/lists");
        Assert.NotNull(lists);
        var repertoireListId = lists.Single(l => l.Kind == SingerListKind.MyRepertoire).Id;

        var (unperformedSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var add = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{repertoireListId}/songs",
            new AddSingerListSongRequest { SongId = unperformedSongId });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var response = await client.GetFromJsonAsync<StaleSongsResponseDto>(
            "/api/performances/my-stale-songs?days=90&limit=5");

        Assert.NotNull(response);
        var song = Assert.Single(response.Songs, s => s.SongId == unperformedSongId);
        Assert.Null(song.LastPerformedOn);
        Assert.Equal(0, song.PerformanceCount);
        Assert.Equal(0, song.DaysSinceLastPerformed);
    }

    [SkippableFact]
    public async Task GetMyStaleSongs_ReturnsRandomSubsetWhenMultipleSongsQualify()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (_, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var staleSongIds = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
            staleSongIds.Add(songId);
            await PerformanceTestDataHelper.CreatePerformanceAsync(
                client, songId, venueId, DateTime.Today.AddDays(-120 - i));
        }

        var seen = new HashSet<int>();
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var response = await client.GetFromJsonAsync<StaleSongsResponseDto>(
                "/api/performances/my-stale-songs?days=90&limit=1");
            Assert.NotNull(response);
            var song = Assert.Single(response.Songs);
            Assert.Contains(song.SongId, staleSongIds);
            seen.Add(song.SongId);
        }

        Assert.True(seen.Count > 1, "Expected random ordering to surface more than one stale song across repeated requests.");
    }

    [SkippableFact]
    public async Task GetMyStaleSongs_InvalidDays_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.GetAsync("/api/performances/my-stale-songs?days=3");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"stale-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
