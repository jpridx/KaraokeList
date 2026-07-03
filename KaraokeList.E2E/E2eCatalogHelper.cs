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
}
