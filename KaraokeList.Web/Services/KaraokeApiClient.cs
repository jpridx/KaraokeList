using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IKaraokeApiClient
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
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
    Task<List<PerformanceDto>> GetPerformancesAsync(int? singerId = null, int? songId = null);
    Task<UserProfileDto?> GetProfileAsync();
    Task<AuthResult> LinkSingerAsync(LinkSingerRequest request);
    Task<SongSummaryResult> GetMySongSummaryAsync(int songId);
    Task CreatePerformanceAsync(PerformanceDto dto);
    Task UpdatePerformanceAsync(PerformanceDto dto);
    Task DeletePerformanceAsync(int id);
}

public sealed class KaraokeApiClient(HttpClient http) : IKaraokeApiClient
{
    public Task<AuthResult> LoginAsync(LoginRequest request) =>
        PostAuthAsync("api/auth/login", request);

    public Task<AuthResult> RegisterAsync(RegisterRequest request) =>
        PostAuthAsync("api/auth/register", request);

    private async Task<AuthResult> PostAuthAsync(string url, object request)
    {
        try
        {
            var response = await http.PostAsJsonAsync(url, request);
            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return auth is null
                    ? AuthResult.Fail("Unexpected empty response from the server.")
                    : AuthResult.Ok(auth);
            }

            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            var message = error?.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Invalid email or password."
                    : $"Request failed ({(int)response.StatusCode}). Is the API running at {http.BaseAddress}?";
            }

            return AuthResult.Fail(message);
        }
        catch (HttpRequestException ex)
        {
            return AuthResult.Fail($"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
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

    public Task<List<PerformanceDto>> GetPerformancesAsync(int? singerId = null, int? songId = null)
    {
        var query = new List<string>();
        if (singerId is int singer) query.Add($"singerId={singer}");
        if (songId is int song) query.Add($"songId={song}");
        var suffix = query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
        return GetListAsync<PerformanceDto>($"api/performances{suffix}");
    }

    public async Task<UserProfileDto?> GetProfileAsync() =>
        await http.GetFromJsonAsync<UserProfileDto>("api/auth/me");

    public Task<AuthResult> LinkSingerAsync(LinkSingerRequest request) =>
        PostAuthAsync("api/auth/link-singer", request);

    public async Task<SongSummaryResult> GetMySongSummaryAsync(int songId)
    {
        var response = await http.GetAsync($"api/performances/my-song-summary?songId={songId}");
        if (response.IsSuccessStatusCode)
        {
            var summary = await response.Content.ReadFromJsonAsync<SongPerformanceSummaryDto>();
            return summary is null
                ? SongSummaryResult.Fail("Unexpected empty response from the server.")
                : SongSummaryResult.Ok(summary);
        }

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        return SongSummaryResult.Fail(error?.Message ?? "Could not load song summary.");
    }

    public Task CreatePerformanceAsync(PerformanceDto dto) => PostAsync("api/performances", dto);
    public Task UpdatePerformanceAsync(PerformanceDto dto) => PutAsync($"api/performances/{dto.Id}", dto);
    public Task DeletePerformanceAsync(int id) => DeleteAsync($"api/performances/{id}");

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
