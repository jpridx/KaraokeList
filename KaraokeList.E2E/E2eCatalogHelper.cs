using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.E2E;

internal static class E2eCatalogHelper
{
    public static async Task<(int SongId, string SongTitle)> SeedSongAsync(
        HttpClient apiClient,
        string token,
        string? explicitSongTitle = null)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var artistName = $"E2E Artist {Guid.NewGuid():N}";
        var createArtist = await apiClient.PostAsJsonAsync("/api/artists", new ArtistDto { Name = artistName });
        if (!createArtist.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create artist failed ({(int)createArtist.StatusCode}).");
        }

        var artists = await apiClient.GetFromJsonAsync<List<ArtistLookupDto>>("/api/artists/lookup")
            ?? throw new InvalidOperationException("Artist lookup returned null.");
        var artistId = artists.First(a => a.Name == artistName).Id;

        var songTitle = explicitSongTitle ?? $"E2E Song {Guid.NewGuid():N}";
        var createSong = await apiClient.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = songTitle,
            Artist = artistId
        });
        if (!createSong.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create song failed ({(int)createSong.StatusCode}).");
        }

        var songs = await apiClient.GetFromJsonAsync<List<SongDto>>("/api/songs")
            ?? throw new InvalidOperationException("Songs list returned null.");
        var songId = songs.First(s => s.Title == songTitle && s.Artist == artistId).Id;
        return (songId, songTitle);
    }

    public static async Task<(int VenueId, string VenueName)> SeedVenueAsync(
        HttpClient apiClient,
        string token,
        string? explicitVenueName = null)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var venueName = explicitVenueName ?? $"E2E Venue {Guid.NewGuid():N}";
        var createVenue = await apiClient.PostAsJsonAsync("/api/venues", new VenueDto { VenueName = venueName });
        if (!createVenue.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create venue failed ({(int)createVenue.StatusCode}).");
        }

        var venues = await apiClient.GetFromJsonAsync<List<VenueDto>>("/api/venues")
            ?? throw new InvalidOperationException("Venues list returned null.");
        var venueId = venues.First(v => v.VenueName == venueName).Id;
        return (venueId, venueName);
    }

    public static async Task<int> SeedPerformanceAsync(
        HttpClient apiClient,
        string token,
        int songId,
        int? venueId = null,
        DateTime? performedOn = null)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        venueId ??= (await SeedVenueAsync(apiClient, token)).VenueId;
        var performed = performedOn ?? DateTime.Today;

        var create = await apiClient.PostAsJsonAsync("/api/performances", new PerformanceDto
        {
            Song = songId,
            Venue = venueId,
            PerformedOn = performed
        });
        if (!create.IsSuccessStatusCode)
        {
            var body = await create.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Create performance failed ({(int)create.StatusCode}): {body}");
        }

        var performances = await apiClient.GetFromJsonAsync<List<PerformanceDto>>("/api/performances")
            ?? throw new InvalidOperationException("Performances list returned null.");
        var created = performances.FirstOrDefault(p => p.Song == songId && p.PerformedOn == performed)
            ?? throw new InvalidOperationException("Created performance was not found.");
        return created.Id;
    }

    public static async Task AddSongsToListAsync(
        HttpClient apiClient,
        string token,
        SingerListKind listKind,
        IReadOnlyList<int> songIds)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await apiClient.PostAsJsonAsync(
            "/api/singers/me/lists/import",
            new ImportSingerListSongsRequest
            {
                ListKind = listKind,
                SongIds = songIds.ToList()
            });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Add songs to list failed ({(int)response.StatusCode}): {body}");
        }
    }

    public static async Task<int> FindSongIdByTitleAsync(
        HttpClient apiClient,
        string token,
        string songTitle)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var songs = await apiClient.GetFromJsonAsync<List<SongDto>>("/api/songs")
            ?? throw new InvalidOperationException("Songs list returned null.");
        return songs.First(s => s.Title == songTitle).Id;
    }

    public static async Task<DateTime> GetPerformanceDateForSongAsync(
        HttpClient apiClient,
        string token,
        string songTitle)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var history = await apiClient.GetFromJsonAsync<List<MyPerformanceEntryDto>>("/api/performances/my-history")
            ?? throw new InvalidOperationException("Performance history returned null.");
        return history.First(h => h.Title == songTitle).PerformedOn;
    }

    public static async Task<int?> GetPerformanceKeyForSongAsync(
        HttpClient apiClient,
        string token,
        string songTitle)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var history = await apiClient.GetFromJsonAsync<List<MyPerformanceEntryDto>>("/api/performances/my-history")
            ?? throw new InvalidOperationException("Performance history returned null.");
        return history.First(h => h.Title == songTitle).KeyChangeSemitones;
    }
}
