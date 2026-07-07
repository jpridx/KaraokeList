using System.Net;
using System.Net.Http.Headers;
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

        var artistName = $"Import Artist {Guid.NewGuid():N}";
        var createArtist = await client.PostAsJsonAsync("/api/artists", new ArtistDto { Name = artistName });
        Assert.Equal(HttpStatusCode.NoContent, createArtist.StatusCode);

        var artists = await client.GetFromJsonAsync<List<ArtistDto>>("/api/artists");
        Assert.NotNull(artists);
        var artistId = Assert.Single(artists, a => a.Name == artistName).Id;

        var titleA = $"Import Song A {Guid.NewGuid():N}";
        var titleB = $"Import Song B {Guid.NewGuid():N}";
        foreach (var title in new[] { titleA, titleB })
        {
            var createSong = await client.PostAsJsonAsync("/api/songs", new SongDto
            {
                Title = title,
                Artist = artistId
            });
            Assert.Equal(HttpStatusCode.NoContent, createSong.StatusCode);
        }

        var songs = await client.GetFromJsonAsync<List<SongDto>>("/api/songs");
        Assert.NotNull(songs);
        var songIdA = Assert.Single(songs, s => s.Title == titleA).Id;
        var songIdB = Assert.Single(songs, s => s.Title == titleB).Id;

        var csv = new StringBuilder()
            .AppendLine("Song,Artist")
            .AppendLine($"{titleA},{artistName}")
            .AppendLine($"{titleB},{artistName}")
            .AppendLine($"{titleA},{artistName}")
            .AppendLine($"Missing Song {Guid.NewGuid():N},{artistName}")
            .ToString();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8, "text/csv"), "file", "repertoire.csv");
        content.Add(new StringContent(nameof(SingerListKind.MyRepertoire)), "listKind");

        var response = await client.PostAsync("/api/singers/me/lists/import/file", content);
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

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8, "text/csv"), "file", "empty.csv");
        content.Add(new StringContent(nameof(SingerListKind.MyRepertoire)), "listKind");

        var response = await client.PostAsync("/api/singers/me/lists/import/file", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ImportSingerListFromFileResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body.TotalRows);
        Assert.Equal(0, body.Matched);
        Assert.Equal(1, body.NotFound);
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
