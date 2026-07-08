using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KaraokeList.Shared;

namespace KaraokeList.E2E;

internal static class E2eCatalogHelper
{
    public static async Task<(int SongId, string SongTitle, string ArtistName)> SeedSongAsync(
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

        var created = await createSong.Content.ReadFromJsonAsync<SongDto>()
            ?? throw new InvalidOperationException("Create song returned null.");
        return (created.Id, songTitle, artistName);
    }

    public static async Task<ImportSingerListFromFileResponse> ImportCsvFileAsync(
        HttpClient apiClient,
        string token,
        string csvContent,
        SingerListKind listKind,
        string fileName = "e2e-import.csv")
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent)), "file", fileName);
        content.Add(new StringContent(listKind.ToString()), "listKind");

        var response = await apiClient.PostAsync("/api/singers/me/lists/import-file", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"CSV import failed ({(int)response.StatusCode}): {body}");
        }

        return System.Text.Json.JsonSerializer.Deserialize<ImportSingerListFromFileResponse>(
            body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Import response was null.");
    }

    public static async Task<bool> PerformanceExistsForSongAsync(
        HttpClient apiClient,
        string token,
        string songTitle)
    {
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var history = await apiClient.GetFromJsonAsync<List<MyPerformanceEntryDto>>("/api/performances/my-history")
            ?? throw new InvalidOperationException("Performance history returned null.");
        return history.Any(h => h.Title == songTitle);
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
