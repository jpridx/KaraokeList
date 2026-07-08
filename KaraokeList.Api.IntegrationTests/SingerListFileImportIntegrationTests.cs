using System.Net;
using System.Net.Http.Json;
using System.Text;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class SingerListFileImportIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task ImportFile_AddsMatchedSongsToMyRepertoire()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var repertoireListId = lists.Single(l => l.Kind == SingerListKind.MyRepertoire).Id;

        var (songIdA, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (songIdB, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        var songs = await client.GetFromJsonAsync<List<SongDto>>("/api/songs");
        var artists = await client.GetFromJsonAsync<List<ArtistDto>>("/api/artists");
        Assert.NotNull(songs);
        Assert.NotNull(artists);

        var songA = songs.Single(s => s.Id == songIdA);
        var songB = songs.Single(s => s.Id == songIdB);
        var artistA = artists.Single(a => a.Id == songA.Artist);
        var artistB = artists.Single(a => a.Id == songB.Artist);

        var csv = new StringBuilder()
            .AppendLine("Song,Artist")
            .AppendLine($"{songA.Title},{artistA.Name}")
            .AppendLine($"{songB.Title},{artistB.Name}")
            .AppendLine($"{songA.Title},{artistA.Name}")
            .AppendLine($"Missing Song {Guid.NewGuid():N},{artistA.Name}")
            .ToString();

        using var content = BuildImportForm(csv, SingerListKind.MyRepertoire);
        var response = await client.PostAsync("/api/singers/me/lists/import-file", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportSingerListFromFileResponse>();
        Assert.NotNull(body);
        Assert.Equal(4, body.TotalRows);
        Assert.Equal(3, body.Matched);
        Assert.Equal(1, body.NotFound);
        Assert.Equal(2, body.Added);
        Assert.Equal(0, body.Skipped);
        Assert.Equal(0, body.Rejected);

        var repertoire = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{repertoireListId}/songs");
        Assert.NotNull(repertoire);
        Assert.Contains(repertoire, s => s.SongId == songIdA);
        Assert.Contains(repertoire, s => s.SongId == songIdB);
    }

    [SkippableFact]
    public async Task ImportFile_NoMatches_ReturnsBadRequestWithDetails()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var csv = "Song,Artist\nNobody Knows This,Unknown Artist\n";

        using var content = BuildImportForm(csv, SingerListKind.MyRepertoire);
        var response = await client.PostAsync("/api/singers/me/lists/import-file", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportSingerListFromFileResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.TotalRows);
        Assert.Equal(0, body.Matched);
        Assert.Equal(1, body.NotFound);
    }

    private static MultipartFormDataContent BuildImportForm(string csv, SingerListKind listKind)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
        content.Add(fileContent, "file", "repertoire.csv");
        content.Add(new StringContent(listKind.ToString()), "listKind");
        return content;
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
        var email = $"file-import-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
