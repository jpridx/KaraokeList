using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IKaraokeApiClient
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RegisterAsync(RegisterRequest request);
    Task<List<VenueDto>> GetVenuesAsync();
    Task CreateVenueAsync(VenueDto dto);
    Task UpdateVenueAsync(VenueDto dto);
    Task DeleteVenueAsync(int id);
    Task<List<GenreDto>> GetGenresAsync();
    Task CreateGenreAsync(GenreDto dto);
    Task UpdateGenreAsync(GenreDto dto);
    Task DeleteGenreAsync(int id);
    Task<List<ArtistDto>> GetArtistsAsync();
    Task<List<ArtistLookupDto>> GetArtistLookupsAsync();
    Task CreateArtistAsync(ArtistDto dto);
    Task UpdateArtistAsync(ArtistDto dto);
    Task DeleteArtistAsync(int id);
    Task<List<SingerDto>> GetSingersAsync();
    Task CreateSingerAsync(SingerDto dto);
    Task UpdateSingerAsync(SingerDto dto);
    Task DeleteSingerAsync(int id);
    Task<List<SongDto>> GetSongsAsync();
    Task CreateSongAsync(SongDto dto);
    Task UpdateSongAsync(SongDto dto);
    Task DeleteSongAsync(int id);
    Task<List<SingerSongDto>> GetSingerSongsAsync();
    Task CreateSingerSongAsync(SingerSongDto dto);
    Task UpdateSingerSongAsync(SingerSongDto dto);
    Task DeleteSingerSongAsync(int id);
}

public sealed class KaraokeApiClient(HttpClient http) : IKaraokeApiClient
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>();
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/register", request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>();
    }

    public Task<List<VenueDto>> GetVenuesAsync() => GetListAsync<VenueDto>("api/venues");
    public Task CreateVenueAsync(VenueDto dto) => PostAsync("api/venues", dto);
    public Task UpdateVenueAsync(VenueDto dto) => PutAsync($"api/venues/{dto.Id}", dto);
    public Task DeleteVenueAsync(int id) => DeleteAsync($"api/venues/{id}");

    public Task<List<GenreDto>> GetGenresAsync() => GetListAsync<GenreDto>("api/genres");
    public Task CreateGenreAsync(GenreDto dto) => PostAsync("api/genres", dto);
    public Task UpdateGenreAsync(GenreDto dto) => PutAsync($"api/genres/{dto.Id}", dto);
    public Task DeleteGenreAsync(int id) => DeleteAsync($"api/genres/{id}");

    public Task<List<ArtistDto>> GetArtistsAsync() => GetListAsync<ArtistDto>("api/artists");
    public Task<List<ArtistLookupDto>> GetArtistLookupsAsync() => GetListAsync<ArtistLookupDto>("api/artists/lookup");
    public Task CreateArtistAsync(ArtistDto dto) => PostAsync("api/artists", dto);
    public Task UpdateArtistAsync(ArtistDto dto) => PutAsync($"api/artists/{dto.Id}", dto);
    public Task DeleteArtistAsync(int id) => DeleteAsync($"api/artists/{id}");

    public Task<List<SingerDto>> GetSingersAsync() => GetListAsync<SingerDto>("api/singers");
    public Task CreateSingerAsync(SingerDto dto) => PostAsync("api/singers", dto);
    public Task UpdateSingerAsync(SingerDto dto) => PutAsync($"api/singers/{dto.Id}", dto);
    public Task DeleteSingerAsync(int id) => DeleteAsync($"api/singers/{id}");

    public Task<List<SongDto>> GetSongsAsync() => GetListAsync<SongDto>("api/songs");
    public Task CreateSongAsync(SongDto dto) => PostAsync("api/songs", dto);
    public Task UpdateSongAsync(SongDto dto) => PutAsync($"api/songs/{dto.Id}", dto);
    public Task DeleteSongAsync(int id) => DeleteAsync($"api/songs/{id}");

    public Task<List<SingerSongDto>> GetSingerSongsAsync() => GetListAsync<SingerSongDto>("api/SingerSongs");
    public Task CreateSingerSongAsync(SingerSongDto dto) => PostAsync("api/SingerSongs", dto);
    public Task UpdateSingerSongAsync(SingerSongDto dto) => PutAsync($"api/SingerSongs/{dto.Id}", dto);
    public Task DeleteSingerSongAsync(int id) => DeleteAsync($"api/SingerSongs/{id}");

    private async Task<List<T>> GetListAsync<T>(string url)
    {
        var result = await http.GetFromJsonAsync<List<T>>(url);
        return result ?? [];
    }

    private async Task PostAsync<T>(string url, T dto)
    {
        var response = await http.PostAsJsonAsync(url, dto);
        response.EnsureSuccessStatusCode();
    }

    private async Task PutAsync<T>(string url, T dto)
    {
        var response = await http.PutAsJsonAsync(url, dto);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteAsync(string url)
    {
        var response = await http.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }
}
