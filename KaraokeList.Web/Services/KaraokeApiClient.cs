using System.Net.Http.Json;
using System.Text.Json;
using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface IKaraokeApiClient
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<RegistrationInfoDto?> GetRegistrationInfoAsync();
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
    Task<RepertoireResult> GetMyRepertoireAsync(
        string sortBy = "lastPerformed",
        string sortDir = "desc",
        int? genreId = null,
        bool includeAll = false);
    Task<RepertoireGenresResult> GetMyRepertoireGenresAsync();
    Task CreatePerformanceAsync(PerformanceDto dto);
    Task UpdatePerformanceAsync(PerformanceDto dto);
    Task DeletePerformanceAsync(int id);
}

public sealed class KaraokeApiClient(HttpClient http) : IKaraokeApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<AuthResult> LoginAsync(LoginRequest request) =>
        PostAuthAsync("api/auth/login", request);

    public Task<AuthResult> RegisterAsync(RegisterRequest request) =>
        PostAuthAsync("api/auth/register", request);

    public async Task<RegistrationInfoDto?> GetRegistrationInfoAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<RegistrationInfoDto>("api/auth/registration", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task<AuthResult> PostAuthAsync(string url, object request)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
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

                if (ApiTransientFailure.IsTransient(response.StatusCode) && attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    continue;
                }

                var message = await ReadApiErrorMessageAsync(response);
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Invalid email or password."
                        : ApiTransientFailure.IsTransient(response.StatusCode)
                            ? ApiTransientFailure.ColdStartMessage
                            : $"Request failed ({(int)response.StatusCode}). Is the API running at {http.BaseAddress}?";
                }

                return AuthResult.Fail(message, ApiTransientFailure.IsTransient(response.StatusCode));
            }
            catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) && attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
            {
                return AuthResult.Fail(ApiTransientFailure.ColdStartMessage, transient: true);
            }
            catch (HttpRequestException ex)
            {
                return AuthResult.Fail(
                    $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
            }
        }

        return AuthResult.Fail(ApiTransientFailure.ColdStartMessage, transient: true);
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

        var message = await ReadApiErrorMessageAsync(response);
        return SongSummaryResult.Fail(message ?? "Could not load song summary.");
    }

    public async Task<RepertoireResult> GetMyRepertoireAsync(
        string sortBy = "lastPerformed",
        string sortDir = "desc",
        int? genreId = null,
        bool includeAll = false)
    {
        var query = $"sortBy={Uri.EscapeDataString(sortBy)}&sortDir={Uri.EscapeDataString(sortDir)}";
        if (genreId is int genre)
        {
            query += $"&genreId={genre}";
        }

        if (includeAll)
        {
            query += "&includeAll=true";
        }

        var response = await http.GetAsync($"api/performances/my-repertoire?{query}");
        if (response.IsSuccessStatusCode)
        {
            var songs = await response.Content.ReadFromJsonAsync<List<RepertoireSongDto>>();
            return RepertoireResult.Ok(songs ?? []);
        }

        var message = await ReadApiErrorMessageAsync(response);
        return RepertoireResult.Fail(message ?? "Could not load repertoire.");
    }

    public async Task<RepertoireGenresResult> GetMyRepertoireGenresAsync()
    {
        var response = await http.GetAsync("api/performances/my-repertoire/genres");
        if (response.IsSuccessStatusCode)
        {
            var genres = await response.Content.ReadFromJsonAsync<List<GenreDto>>();
            return RepertoireGenresResult.Ok(genres ?? []);
        }

        var message = await ReadApiErrorMessageAsync(response);
        return RepertoireGenresResult.Fail(message ?? "Could not load genres.");
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

    private static async Task<string?> ReadApiErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
            // Developer exception page or other non-JSON error body
        }

        return body.Length <= 200 ? body.Trim() : null;
    }
}
