using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Api.Services;
using KaraokeList.Api.Services.Import;
using KaraokeList.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class SingerListFileImportIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task ImportRows_AddsMatchedSongsToMyRepertoire()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var singerId = await RequireSingerIdAsync(client);
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
        var artistA = artists.Single(a => a.Id == songA.Artists.First().ArtistId);
        var artistB = artists.Single(a => a.Id == songB.Artists.First().ArtistId);

        using var scope = factory.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<RepertoireImportService>();
        var rows = new List<CatalogImportRow>
        {
            new(songA.Title, artistA.Name, null, null, 2),
            new(songB.Title, artistB.Name, null, null, 3),
            new(songA.Title, artistA.Name, null, null, 4),
            new($"Missing Song {Guid.NewGuid():N}", artistA.Name, null, null, 5)
        };

        var result = await importService.ImportRowsAsync(singerId, SingerListKind.MyRepertoire, rows);
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Result);
        Assert.Equal(4, result.Result.TotalRows);
        Assert.Equal(3, result.Result.Matched);
        Assert.Equal(1, result.Result.NotFound);
        Assert.Equal(2, result.Result.Added);
        Assert.Equal(0, result.Result.Skipped);
        Assert.Equal(0, result.Result.Rejected);

        var repertoire = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{repertoireListId}/songs");
        Assert.NotNull(repertoire);
        Assert.Contains(repertoire, s => s.SongId == songIdA);
        Assert.Contains(repertoire, s => s.SongId == songIdB);
    }

    [SkippableFact]
    public async Task ImportRows_NoMatches_ReturnsErrorWithDetails()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var singerId = await RequireSingerIdAsync(client);

        using var scope = factory.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<RepertoireImportService>();
        var rows = new List<CatalogImportRow>
        {
            new("Nobody Knows This", "Unknown Artist", null, null, 2)
        };

        var result = await importService.ImportRowsAsync(singerId, SingerListKind.MyRepertoire, rows);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Result);
        Assert.Equal(1, result.Result.TotalRows);
        Assert.Equal(0, result.Result.Matched);
        Assert.Equal(1, result.Result.NotFound);
    }

    private static async Task<int> RequireSingerIdAsync(HttpClient client)
    {
        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/auth/me");
        Assert.NotNull(profile?.SingerId);
        return profile.SingerId.Value;
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
