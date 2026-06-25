using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class SingerListImportIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task Import_AddsMultipleSongsToMyRepertoire()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var repertoireListId = lists.Single(l => l.Kind == SingerListKind.MyRepertoire).Id;

        var (songIdA, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (songIdB, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/singers/me/lists/import",
            new ImportSingerListSongsRequest
            {
                ListKind = SingerListKind.MyRepertoire,
                SongIds = [songIdA, songIdB, songIdB]
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ImportSingerListSongsResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Added);
        Assert.Equal(1, body.Skipped);
        Assert.Equal(0, body.Rejected);

        var songs = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{repertoireListId}/songs");
        Assert.NotNull(songs);
        Assert.Contains(songs, s => s.SongId == songIdA);
        Assert.Contains(songs, s => s.SongId == songIdB);
    }

    [SkippableFact]
    public async Task Import_ToWantToSing_RejectsPerformedSongs()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (performedSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (unperformedSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, performedSongId, venueId);

        var response = await client.PostAsJsonAsync(
            "/api/singers/me/lists/import",
            new ImportSingerListSongsRequest
            {
                ListKind = SingerListKind.WantToSing,
                SongIds = [performedSongId, unperformedSongId]
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ImportSingerListSongsResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.Added);
        Assert.Equal(0, body.Skipped);
        Assert.Equal(1, body.Rejected);
    }

    [SkippableFact]
    public async Task Import_EmptySongIds_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.PostAsJsonAsync(
            "/api/singers/me/lists/import",
            new ImportSingerListSongsRequest
            {
                ListKind = SingerListKind.MyRepertoire,
                SongIds = []
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<List<SingerListDto>> GetListsAsync(HttpClient client)
    {
        var lists = await client.GetFromJsonAsync<List<SingerListDto>>("/api/singers/me/lists");
        Assert.NotNull(lists);
        return lists;
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"import-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
