using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class MyRepertoireIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetMyRepertoire_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/performances/my-repertoire");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetMyRepertoire_ReturnsOnlyCurrentSingerSongs()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var clientA = await CreateAuthedClientAsync();
        var clientB = await CreateAuthedClientAsync();

        var (songIdA, venueIdA) = await PerformanceTestDataHelper.CreateCatalogAsync(clientA);
        var (songIdB, venueIdB) = await PerformanceTestDataHelper.CreateCatalogAsync(clientB);

        await PerformanceTestDataHelper.CreatePerformanceAsync(clientA, songIdA, venueIdA);
        await PerformanceTestDataHelper.CreatePerformanceAsync(clientB, songIdB, venueIdB);

        var repertoireA = await clientA.GetFromJsonAsync<List<RepertoireSongDto>>("/api/performances/my-repertoire");
        Assert.NotNull(repertoireA);
        Assert.Contains(repertoireA, s => s.SongId == songIdA);
        Assert.DoesNotContain(repertoireA, s => s.SongId == songIdB);
        Assert.Equal(1, Assert.Single(repertoireA, s => s.SongId == songIdA).PerformanceCount);
    }

    [SkippableFact]
    public async Task GetMyRepertoire_IncludeAll_IncludesUnperformedCatalogSongs()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (performedSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (unperformedSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        await PerformanceTestDataHelper.CreatePerformanceAsync(client, performedSongId, venueId);

        var repertoire = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            "/api/performances/my-repertoire?includeAll=true");

        Assert.NotNull(repertoire);
        Assert.Equal(1, Assert.Single(repertoire, s => s.SongId == performedSongId).PerformanceCount);
        Assert.Equal(0, Assert.Single(repertoire, s => s.SongId == unperformedSongId).PerformanceCount);
        Assert.Null(Assert.Single(repertoire, s => s.SongId == unperformedSongId).LastPerformedOn);
    }

    [SkippableFact]
    public async Task GetMyRepertoire_InvalidSortBy_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.GetAsync("/api/performances/my-repertoire?sortBy=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body?.Message);
        Assert.Contains("Invalid sortBy", body.Message);
    }

    [SkippableFact]
    public async Task GetMyRepertoireGenres_ReturnsDistinctGenresFromRepertoire()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var rockId = await PerformanceTestDataHelper.CreateGenreAsync(client, $"Rock {Guid.NewGuid():N}");
        var popId = await PerformanceTestDataHelper.CreateGenreAsync(client, $"Pop {Guid.NewGuid():N}");

        var (rockSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client, rockId);
        var (popSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client, popId);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, rockSongId, venueId);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, popSongId, venueId);

        var genres = await client.GetFromJsonAsync<List<GenreDto>>("/api/performances/my-repertoire/genres");
        Assert.NotNull(genres);
        Assert.Contains(genres, g => g.Id == rockId);
        Assert.Contains(genres, g => g.Id == popId);
    }

    [SkippableFact]
    public async Task GetMySongSummary_ReturnsCountKeyVenueAndHistory()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var older = DateTime.Today.AddDays(-7);
        var newer = DateTime.Today.AddDays(-1);

        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songId, venueId, older, keyChangeSemitones: 0);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songId, venueId, newer, keyChangeSemitones: 2);

        var summary = await client.GetFromJsonAsync<SongPerformanceSummaryDto>(
            $"/api/performances/my-song-summary?songId={songId}");

        Assert.NotNull(summary);
        Assert.Equal(songId, summary.SongId);
        Assert.Equal(2, summary.PerformanceCount);
        Assert.Equal(2, summary.LastKeyChangeSemitones);
        Assert.Equal(newer, summary.LastPerformedOn?.Date);
        Assert.Equal(2, summary.History.Count);
        Assert.Equal(newer, summary.History[0].PerformedOn.Date);
        Assert.Equal(2, summary.History[0].KeyChangeSemitones);
    }

    [SkippableFact]
    public async Task GetMySongSummary_UnperformedSong_ReturnsZeroCount()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var summary = await client.GetFromJsonAsync<SongPerformanceSummaryDto>(
            $"/api/performances/my-song-summary?songId={songId}");

        Assert.NotNull(summary);
        Assert.Equal(songId, summary.SongId);
        Assert.Equal(0, summary.PerformanceCount);
        Assert.Empty(summary.History);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"repertoire-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
