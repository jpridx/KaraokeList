using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class SongSearchIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task Search_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/songs/search?q=anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task Search_WithNoFilters_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.GetAsync("/api/songs/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(body?.Message);
    }

    [SkippableFact]
    public async Task Search_WithWhitespaceOnlyQueryAndNoOtherFilters_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.GetAsync("/api/songs/search?q=%20%20");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task Search_WhenNoMatches_ReturnsEmptyList()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var unique = Guid.NewGuid().ToString("N");

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?q=zzzz-{unique}");

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [SkippableFact]
    public async Task Search_ByTitle_ReturnsMatchingSong()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];
        var songTitle = $"SearchHit-{token} Night";

        var (_, createdSong) = await CreateArtistAndSongAsync(client, songTitle: songTitle);

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?q=SearchHit-{token}");

        Assert.NotNull(results);
        Assert.Contains(results, s => s.Id == createdSong.Id && s.Title == songTitle);
    }

    [SkippableFact]
    public async Task Search_ByArtistName_ReturnsMatchingSong()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];
        var artistName = $"Journey-{token}";
        var songTitle = $"Unrelated Title {Guid.NewGuid():N}";

        var (_, createdSong) = await CreateArtistAndSongAsync(client, artistName: artistName, songTitle: songTitle);

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?q=Journey-{token}");

        Assert.NotNull(results);
        Assert.Contains(results, s => s.Id == createdSong.Id);
    }

    [SkippableFact]
    public async Task Search_ByArtistCreditDisplay_ReturnsMatchingSong()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];
        var artistName = $"Primary {Guid.NewGuid():N}";
        var creditDisplay = $"GuestCredit-{token}";

        var (_, createdSong) = await CreateArtistAndSongAsync(
            client,
            artistName: artistName,
            songTitle: $"Song {Guid.NewGuid():N}",
            artistCreditDisplay: creditDisplay);

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?q=GuestCredit-{token}");

        Assert.NotNull(results);
        Assert.Contains(results, s => s.Id == createdSong.Id);
    }

    [SkippableFact]
    public async Task Search_ByArtistIdOnly_ReturnsSongsForThatArtist()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];

        var (artistAId, songA) = await CreateArtistAndSongAsync(
            client,
            artistName: $"ArtistA-{token}",
            songTitle: $"SongA-{token}");
        var (_, songB) = await CreateArtistAndSongAsync(
            client,
            artistName: $"ArtistB-{token}",
            songTitle: $"SongB-{token}");

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?artistId={artistAId}");

        Assert.NotNull(results);
        Assert.Contains(results, s => s.Id == songA.Id);
        Assert.DoesNotContain(results, s => s.Id == songB.Id);
    }

    [SkippableFact]
    public async Task Search_ByGenreId_ReturnsSongsInThatGenre()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (adminClient, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var token = Guid.NewGuid().ToString("N")[..8];
        var genreA = await PerformanceTestDataHelper.CreateGenreAsync(adminClient, $"GenreA-{token}");
        var genreB = await PerformanceTestDataHelper.CreateGenreAsync(adminClient, $"GenreB-{token}");

        var (_, songInGenreA) = await CreateArtistAndSongAsync(
            client,
            songTitle: $"InGenreA-{token}",
            genreId: genreA);
        var (_, songInGenreB) = await CreateArtistAndSongAsync(
            client,
            songTitle: $"InGenreB-{token}",
            genreId: genreB);

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?genreId={genreA}");

        Assert.NotNull(results);
        Assert.Contains(results, s => s.Id == songInGenreA.Id);
        Assert.DoesNotContain(results, s => s.Id == songInGenreB.Id);
    }

    [SkippableFact]
    public async Task Search_WithQueryAndArtistId_NarrowsToSongsMatchingBoth()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];

        var (artistAId, songMatch) = await CreateArtistAndSongAsync(
            client,
            artistName: $"MatchArtist-{token}",
            songTitle: $"MatchTitle-{token}");
        var (_, songWrongArtist) = await CreateArtistAndSongAsync(
            client,
            artistName: $"OtherArtist-{token}",
            songTitle: $"MatchTitle-{token}");

        var results = await client.GetFromJsonAsync<List<SongDto>>(
            $"/api/songs/search?q=MatchTitle-{token}&artistId={artistAId}");

        Assert.NotNull(results);
        Assert.Contains(results, s => s.Id == songMatch.Id);
        Assert.DoesNotContain(results, s => s.Id == songWrongArtist.Id);
    }

    [SkippableFact]
    public async Task Search_WithTake_ReturnsAtMostRequestedCount()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];
        var prefix = $"TakeTest-{token}";

        await CreateArtistAndSongAsync(client, songTitle: $"{prefix}-1");
        await CreateArtistAndSongAsync(client, songTitle: $"{prefix}-2");
        await CreateArtistAndSongAsync(client, songTitle: $"{prefix}-3");

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?q={prefix}&take=2");

        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Contains(prefix, s.Title));
    }

    [SkippableFact]
    public async Task Search_WithTakeOutsideRange_IsClamped()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];
        var prefix = $"ClampTest-{token}";

        await CreateArtistAndSongAsync(client, songTitle: $"{prefix}-1");
        await CreateArtistAndSongAsync(client, songTitle: $"{prefix}-2");

        var zeroTake = await client.GetAsync($"/api/songs/search?q={prefix}&take=0");
        Assert.Equal(HttpStatusCode.OK, zeroTake.StatusCode);
        var zeroResults = await zeroTake.Content.ReadFromJsonAsync<List<SongDto>>();
        Assert.NotNull(zeroResults);
        Assert.Single(zeroResults);

        var highTake = await client.GetAsync($"/api/songs/search?q={prefix}&take=999");
        Assert.Equal(HttpStatusCode.OK, highTake.StatusCode);
        var highResults = await highTake.Content.ReadFromJsonAsync<List<SongDto>>();
        Assert.NotNull(highResults);
        Assert.Equal(2, highResults.Count);
    }

    [SkippableFact]
    public async Task Search_ReturnsSongDtoWithArtistCredits()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var token = Guid.NewGuid().ToString("N")[..8];
        var artistName = $"CreditArtist-{token}";
        var songTitle = $"CreditSong-{token}";

        var (artistId, createdSong) = await CreateArtistAndSongAsync(
            client,
            artistName: artistName,
            songTitle: songTitle);

        var results = await client.GetFromJsonAsync<List<SongDto>>($"/api/songs/search?q=CreditSong-{token}");

        Assert.NotNull(results);
        var hit = Assert.Single(results, s => s.Id == createdSong.Id);
        var credit = Assert.Single(hit.Artists);
        Assert.Equal(artistId, credit.ArtistId);
        Assert.Equal(artistName, credit.Name);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"song-search-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<(int ArtistId, SongDto Song)> CreateArtistAndSongAsync(
        HttpClient client,
        string? artistName = null,
        string? songTitle = null,
        int? genreId = null,
        string? artistCreditDisplay = null)
    {
        artistName ??= $"Artist {Guid.NewGuid():N}";
        songTitle ??= $"Song {Guid.NewGuid():N}";

        var createArtist = await client.PostAsJsonAsync("/api/artists", new ArtistDto { Name = artistName });
        Assert.Equal(HttpStatusCode.NoContent, createArtist.StatusCode);

        var artists = await client.GetFromJsonAsync<List<ArtistDto>>("/api/artists");
        Assert.NotNull(artists);
        var artistId = Assert.Single(artists, a => a.Name == artistName).Id;

        var createSong = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = songTitle,
            Genre = genreId,
            ArtistCreditDisplay = artistCreditDisplay,
            Artists =
            [
                new SongArtistDto { ArtistId = artistId, DisplayOrder = 0, Name = artistName }
            ]
        });
        Assert.Equal(HttpStatusCode.Created, createSong.StatusCode);
        var createdSong = await createSong.Content.ReadFromJsonAsync<SongDto>();
        Assert.NotNull(createdSong);

        return (artistId, createdSong!);
    }
}
