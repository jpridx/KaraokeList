using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class PerformancesScopingTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetPerformances_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/performances");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetPerformances_ReturnsOnlyCurrentSingerPerformances()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var clientA = await CreateAuthedClientAsync();
        var clientB = await CreateAuthedClientAsync();

        var (songIdA, venueIdA) = await PerformanceTestDataHelper.CreateCatalogAsync(clientA);
        var (songIdB, venueIdB) = await PerformanceTestDataHelper.CreateCatalogAsync(clientB);

        var performanceIdA = await PerformanceTestDataHelper.CreatePerformanceAsync(clientA, songIdA, venueIdA);
        var performanceIdB = await PerformanceTestDataHelper.CreatePerformanceAsync(clientB, songIdB, venueIdB);

        var profileB = await clientB.GetFromJsonAsync<UserProfileDto>("/api/auth/me");
        Assert.NotNull(profileB?.SingerId);

        var listA = await clientA.GetFromJsonAsync<List<PerformanceDto>>($"/api/performances?singerId={profileB.SingerId}");
        Assert.NotNull(listA);
        Assert.Contains(listA, p => p.Id == performanceIdA);
        Assert.DoesNotContain(listA, p => p.Id == performanceIdB);

        var listB = await clientB.GetFromJsonAsync<List<PerformanceDto>>("/api/performances");
        Assert.NotNull(listB);
        Assert.Contains(listB, p => p.Id == performanceIdB);
        Assert.DoesNotContain(listB, p => p.Id == performanceIdA);
    }

    [SkippableFact]
    public async Task CreatePerformance_WithOtherSingerId_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var clientA = await CreateAuthedClientAsync();
        var clientB = await CreateAuthedClientAsync();

        var profileB = await clientB.GetFromJsonAsync<UserProfileDto>("/api/auth/me");
        Assert.NotNull(profileB?.SingerId);

        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(clientA);
        var response = await clientA.PostAsJsonAsync("/api/performances", new PerformanceDto
        {
            Singer = profileB.SingerId,
            Song = songId,
            Venue = venueId,
            PerformedOn = DateTime.Today
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task UpdatePerformance_OtherSingersPerformance_ReturnsNotFound()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var clientA = await CreateAuthedClientAsync();
        var clientB = await CreateAuthedClientAsync();

        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(clientA);
        var performanceId = await PerformanceTestDataHelper.CreatePerformanceAsync(clientA, songId, venueId);

        var response = await clientB.PutAsJsonAsync($"/api/performances/{performanceId}", new PerformanceDto
        {
            Id = performanceId,
            Song = songId,
            Venue = venueId,
            PerformedOn = DateTime.Today.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task DeletePerformance_OtherSingersPerformance_ReturnsNotFound()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var clientA = await CreateAuthedClientAsync();
        var clientB = await CreateAuthedClientAsync();

        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(clientA);
        var performanceId = await PerformanceTestDataHelper.CreatePerformanceAsync(clientA, songId, venueId);

        var response = await clientB.DeleteAsync($"/api/performances/{performanceId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var listA = await clientA.GetFromJsonAsync<List<PerformanceDto>>("/api/performances");
        Assert.NotNull(listA);
        Assert.Contains(listA, p => p.Id == performanceId);
    }

    [SkippableFact]
    public async Task GetMyHistory_ReturnsEnrichedRowsForCurrentSingerOnly()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var clientA = await CreateAuthedClientAsync();
        var clientB = await CreateAuthedClientAsync();

        var (songIdA, venueIdA) = await PerformanceTestDataHelper.CreateCatalogAsync(clientA);
        var (songIdB, venueIdB) = await PerformanceTestDataHelper.CreateCatalogAsync(clientB);

        var performanceIdA = await PerformanceTestDataHelper.CreatePerformanceAsync(clientA, songIdA, venueIdA);
        await PerformanceTestDataHelper.CreatePerformanceAsync(clientB, songIdB, venueIdB);

        var songsA = await clientA.GetFromJsonAsync<List<SongDto>>("/api/songs");
        var venuesA = await clientA.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        Assert.NotNull(songsA);
        Assert.NotNull(venuesA);
        var songA = Assert.Single(songsA, s => s.Id == songIdA);
        var venueA = Assert.Single(venuesA, v => v.Id == venueIdA);

        var historyA = await clientA.GetFromJsonAsync<List<MyPerformanceEntryDto>>("/api/performances/my-history");
        Assert.NotNull(historyA);
        var row = Assert.Single(historyA, p => p.Id == performanceIdA);
        Assert.Equal(songIdA, row.SongId);
        Assert.Equal(songA.Title, row.Title);
        Assert.Equal(venueA.VenueName, row.VenueName);
        Assert.DoesNotContain(historyA, p => p.SongId == songIdB);

        var historyB = await clientB.GetFromJsonAsync<List<MyPerformanceEntryDto>>("/api/performances/my-history");
        Assert.NotNull(historyB);
        Assert.DoesNotContain(historyB, p => p.Id == performanceIdA);
    }

    [SkippableFact]
    public async Task CreatePerformance_WithCoPerformers_PersistsAndReturnsOnHistory()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var clientA = await CreateAuthedClientAsync();
        var clientB = await CreateAuthedClientAsync();
        var profileB = await clientB.GetFromJsonAsync<UserProfileDto>("/api/auth/me");
        Assert.NotNull(profileB?.SingerId);

        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(clientA);
        var performanceId = await PerformanceTestDataHelper.CreatePerformanceAsync(
            clientA,
            songId,
            venueId,
            otherPerformers:
            [
                new CoPerformerInputDto { SingerId = profileB.SingerId },
                new CoPerformerInputDto { DisplayName = "Guest Singer" }
            ]);

        var performances = await clientA.GetFromJsonAsync<List<PerformanceDto>>("/api/performances");
        Assert.NotNull(performances);
        var performance = Assert.Single(performances, p => p.Id == performanceId);
        Assert.Equal(2, performance.CoPerformers.Count);
        Assert.Contains(performance.CoPerformers, p => p.SingerId == profileB.SingerId);
        Assert.Contains(performance.CoPerformers, p => p.Name == "Guest Singer");

        var history = await clientA.GetFromJsonAsync<List<MyPerformanceEntryDto>>("/api/performances/my-history");
        Assert.NotNull(history);
        var row = Assert.Single(history, p => p.Id == performanceId);
        Assert.Equal(2, row.OtherPerformers.Count);
        Assert.Contains(row.OtherPerformers, p => p.Name == "Guest Singer");
    }

    [SkippableFact]
    public async Task UpdatePerformance_WithGuestCoPerformer_PersistsAndReturnsOnHistory()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var performanceId = await PerformanceTestDataHelper.CreatePerformanceAsync(client, songId, venueId);

        var update = await client.PutAsJsonAsync($"/api/performances/{performanceId}", new PerformanceDto
        {
            Id = performanceId,
            Song = songId,
            Venue = venueId,
            PerformedOn = DateTime.Today,
            OtherPerformers = [new CoPerformerInputDto { DisplayName = "Guest Singer" }]
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var history = await client.GetFromJsonAsync<List<MyPerformanceEntryDto>>("/api/performances/my-history");
        Assert.NotNull(history);
        var row = Assert.Single(history, p => p.Id == performanceId);
        var guest = Assert.Single(row.OtherPerformers);
        Assert.Equal("Guest Singer", guest.Name);
        Assert.Null(guest.SingerId);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"perf-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

internal static class PerformanceTestDataHelper
{
    public static async Task<int> CreateGenreAsync(HttpClient adminClient, string? genreName = null)
    {
        genreName ??= $"Genre {Guid.NewGuid():N}";
        var createGenre = await adminClient.PostAsJsonAsync("/api/genres", new GenreDto { GenreName = genreName });
        Assert.Equal(HttpStatusCode.NoContent, createGenre.StatusCode);

        var genres = await adminClient.GetFromJsonAsync<List<GenreDto>>("/api/genres");
        Assert.NotNull(genres);
        return Assert.Single(genres, g => g.GenreName == genreName).Id;
    }

    public static async Task<(int SongId, int VenueId)> CreateCatalogAsync(HttpClient client, int? genreId = null)
    {
        var artistName = $"Artist {Guid.NewGuid():N}";
        var createArtist = await client.PostAsJsonAsync("/api/artists", new ArtistDto { Name = artistName });
        Assert.Equal(HttpStatusCode.NoContent, createArtist.StatusCode);

        var artists = await client.GetFromJsonAsync<List<ArtistDto>>("/api/artists");
        Assert.NotNull(artists);
        var artistId = Assert.Single(artists, a => a.Name == artistName).Id;

        var songTitle = $"Song {Guid.NewGuid():N}";
        var createSong = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = songTitle,
            Artist = artistId,
            Genre = genreId
        });
        Assert.Equal(HttpStatusCode.NoContent, createSong.StatusCode);

        var songs = await client.GetFromJsonAsync<List<SongDto>>("/api/songs");
        Assert.NotNull(songs);
        var songId = Assert.Single(songs, s => s.Title == songTitle).Id;

        var venueName = $"Venue {Guid.NewGuid():N}";
        var createVenue = await client.PostAsJsonAsync("/api/venues", new VenueDto { VenueName = venueName });
        Assert.Equal(HttpStatusCode.NoContent, createVenue.StatusCode);

        var venues = await client.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        Assert.NotNull(venues);
        var venueId = Assert.Single(venues, v => v.VenueName == venueName).Id;

        return (songId, venueId);
    }

    public static async Task<int> CreatePerformanceAsync(
        HttpClient client,
        int songId,
        int venueId,
        DateTime? performedOn = null,
        int? keyChangeSemitones = null,
        IReadOnlyList<CoPerformerInputDto>? otherPerformers = null)
    {
        var performed = performedOn ?? DateTime.Today;
        var create = await client.PostAsJsonAsync("/api/performances", new PerformanceDto
        {
            Song = songId,
            Venue = venueId,
            PerformedOn = performed,
            KeyChangeSemitones = keyChangeSemitones,
            OtherPerformers = otherPerformers?.ToList()
        });
        Assert.Equal(HttpStatusCode.NoContent, create.StatusCode);

        var performances = await client.GetFromJsonAsync<List<PerformanceDto>>("/api/performances");
        Assert.NotNull(performances);
        var created = Assert.Single(performances, p => p.Song == songId && p.PerformedOn == performed);
        Assert.True(created.Id > 0);
        return created.Id;
    }
}
