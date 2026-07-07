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
    Task<CatalogMutateResult> TryCreateVenueAsync(VenueDto dto);
    Task UpdateVenueAsync(VenueDto dto);
    Task<CatalogMutateResult> TryUpdateVenueAsync(VenueDto dto);
    Task DeleteVenueAsync(int id);
    Task<CatalogMutateResult> TryDeleteVenueAsync(int id);
    Task<List<GenreDto>> GetGenresAsync();
    Task CreateGenreAsync(GenreDto dto);
    Task<CatalogMutateResult> TryCreateGenreAsync(GenreDto dto);
    Task UpdateGenreAsync(GenreDto dto);
    Task<CatalogMutateResult> TryUpdateGenreAsync(GenreDto dto);
    Task DeleteGenreAsync(int id);
    Task<CatalogMutateResult> TryDeleteGenreAsync(int id);
    Task<List<ArtistDto>> GetArtistsAsync();
    Task<List<ArtistLookupDto>> GetArtistLookupsAsync();
    Task CreateArtistAsync(ArtistDto dto);
    Task<CatalogMutateResult> TryCreateArtistAsync(ArtistDto dto);
    Task UpdateArtistAsync(ArtistDto dto);
    Task<CatalogMutateResult> TryUpdateArtistAsync(ArtistDto dto);
    Task DeleteArtistAsync(int id);
    Task<CatalogMutateResult> TryDeleteArtistAsync(int id);
    Task<List<SingerDto>> GetSingersAsync();
    Task CreateSingerAsync(SingerDto dto);
    Task<CatalogMutateResult> TryCreateSingerAsync(SingerDto dto);
    Task UpdateSingerAsync(SingerDto dto);
    Task<CatalogMutateResult> TryUpdateSingerAsync(SingerDto dto);
    Task DeleteSingerAsync(int id);
    Task<CatalogMutateResult> TryDeleteSingerAsync(int id);
    Task<List<SongDto>> GetSongsAsync();
    Task CreateSongAsync(SongDto dto);
    Task<CatalogMutateResult> TryCreateSongAsync(SongDto dto);
    Task UpdateSongAsync(SongDto dto);
    Task<CatalogMutateResult> TryUpdateSongAsync(SongDto dto);
    Task DeleteSongAsync(int id);
    Task<CatalogMutateResult> TryDeleteSongAsync(int id);
    Task<AppVersionDto?> GetAppVersionAsync();
    Task<CatalogImportFileResult> ImportCatalogFileAsync(Stream fileStream, string fileName);
    Task<CatalogImportFileResult> ImportCatalogFromGSheetAsync(GSheetImportRequest request);
    Task<CatalogMutateResult> MergeSongsAsync(int sourceId, int targetId);
    Task<List<PerformanceDto>> GetPerformancesAsync(int? songId = null);
    Task<UserProfileDto?> GetProfileAsync();
    Task<InviteShareDto?> GetInviteShareAsync();
    Task<AuthResult> LinkSingerAsync(LinkSingerRequest request);
    Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordRequest request);
    Task<PasswordRecoveryResult> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<PasswordRecoveryResult> ResetPasswordAsync(ResetPasswordRequest request);
    Task<SongSummaryResult> GetMySongSummaryAsync(int songId);
    Task<RepertoireResult> GetMyRepertoireAsync(
        string sortBy = "lastPerformed",
        string sortDir = "desc",
        int? genreId = null,
        bool includeAll = false);
    Task<RepertoireGenresResult> GetMyRepertoireGenresAsync();
    Task<SingerListsResult> GetMyListsAsync();
    Task<RepertoireResult> GetListSongsAsync(
        int listId,
        string sortBy = "title",
        string sortDir = "asc",
        int? genreId = null);
    Task<SingerListImportResult> ImportListSongsAsync(ImportSingerListSongsRequest request);
    Task<SingerListFileImportResult> ImportListSongsFromFileAsync(Stream fileStream, string fileName, SingerListKind listKind);
    Task<SingerListFileImportResult> ImportListSongsFromGSheetAsync(ImportSingerListFromGSheetRequest request);
    Task<ListSongActionResult> AddListSongAsync(int listId, int songId);
    Task<ListSongActionResult> RemoveListSongAsync(int listId, int songId);
    Task<SongListMembershipResult> GetSongListMembershipAsync(int songId);
    Task<SongTicklerExclusionResult> GetSongTicklerExclusionAsync(int songId);
    Task<TicklerExclusionActionResult> SetSongTicklerExclusionAsync(int songId, UpdateSongTicklerExclusionRequest request);
    Task<TicklerExclusionActionResult> RemoveSongTicklerExclusionAsync(int songId);
    Task<StaleSongsResult> GetMyStaleSongsAsync(int? days = null, int? limit = null);
    Task<TicklerSettingsResult> GetTicklerSettingsAsync();
    Task<TicklerSettingsUpdateResult> UpdateTicklerSettingsAsync(UpdateTicklerSettingsRequest request);
    Task<MusicServicePreferenceResult> GetMusicServicePreferenceAsync();
    Task<MusicServicePreferenceUpdateResult> UpdateMusicServicePreferenceAsync(UpdateMusicServicePreferenceRequest request);
    Task<SingerStatsResult> GetMySingerStatsAsync(
        int topVenues = 0,
        int topSongs = 0,
        int topArtists = 0,
        int newRepertoireDays = 0);
    Task<MyPerformancesResult> GetMyPerformancesAsync(int? venueId = null, string sortDir = "desc");
    Task CreatePerformanceAsync(PerformanceDto dto);
    Task<PerformanceCreateResult> TryCreatePerformanceAsync(PerformanceDto dto);
    Task UpdatePerformanceAsync(PerformanceDto dto);
    Task<CatalogMutateResult> TryUpdatePerformanceAsync(PerformanceDto dto);
    Task DeletePerformanceAsync(int id);
    Task<CatalogMutateResult> TryDeletePerformanceAsync(int id);
    Task<List<AdminUserDto>> GetAdminUsersAsync();
    Task<AdminUserUpdateResult> UpdateAdminUserAsync(UpdateAdminUserRequest request);
    Task<GenreSuggestionResponse?> SuggestGenreAsync(GenreSuggestionRequest request);
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
    public Task<CatalogMutateResult> TryCreateVenueAsync(VenueDto dto) => TryPostAsync("api/venues", dto);
    public Task UpdateVenueAsync(VenueDto dto) => PutAsync($"api/venues/{dto.Id}", dto);
    public Task<CatalogMutateResult> TryUpdateVenueAsync(VenueDto dto) => TryPutAsync($"api/venues/{dto.Id}", dto);
    public Task DeleteVenueAsync(int id) => DeleteAsync($"api/venues/{id}");
    public Task<CatalogMutateResult> TryDeleteVenueAsync(int id) => TryDeleteAsync($"api/venues/{id}");

    public Task<List<GenreDto>> GetGenresAsync() => GetListAsync<GenreDto>("api/genres");
    public Task CreateGenreAsync(GenreDto dto) => PostAsync("api/genres", dto);
    public Task<CatalogMutateResult> TryCreateGenreAsync(GenreDto dto) => TryPostAsync("api/genres", dto);
    public Task UpdateGenreAsync(GenreDto dto) => PutAsync($"api/genres/{dto.Id}", dto);
    public Task<CatalogMutateResult> TryUpdateGenreAsync(GenreDto dto) => TryPutAsync($"api/genres/{dto.Id}", dto);
    public Task DeleteGenreAsync(int id) => DeleteAsync($"api/genres/{id}");
    public Task<CatalogMutateResult> TryDeleteGenreAsync(int id) => TryDeleteAsync($"api/genres/{id}");

    public Task<List<ArtistDto>> GetArtistsAsync() => GetListAsync<ArtistDto>("api/artists");
    public Task<List<ArtistLookupDto>> GetArtistLookupsAsync() => GetListAsync<ArtistLookupDto>("api/artists/lookup");
    public Task CreateArtistAsync(ArtistDto dto) => PostAsync("api/artists", dto);
    public Task<CatalogMutateResult> TryCreateArtistAsync(ArtistDto dto) => TryPostAsync("api/artists", dto);
    public Task UpdateArtistAsync(ArtistDto dto) => PutAsync($"api/artists/{dto.Id}", dto);
    public Task<CatalogMutateResult> TryUpdateArtistAsync(ArtistDto dto) => TryPutAsync($"api/artists/{dto.Id}", dto);
    public Task DeleteArtistAsync(int id) => DeleteAsync($"api/artists/{id}");
    public Task<CatalogMutateResult> TryDeleteArtistAsync(int id) => TryDeleteAsync($"api/artists/{id}");

    public Task<List<SingerDto>> GetSingersAsync() => GetListAsync<SingerDto>("api/singers");
    public Task CreateSingerAsync(SingerDto dto) => PostAsync("api/singers", dto);
    public Task<CatalogMutateResult> TryCreateSingerAsync(SingerDto dto) => TryPostAsync("api/singers", dto);
    public Task UpdateSingerAsync(SingerDto dto) => PutAsync($"api/singers/{dto.Id}", dto);
    public Task<CatalogMutateResult> TryUpdateSingerAsync(SingerDto dto) => TryPutAsync($"api/singers/{dto.Id}", dto);
    public Task DeleteSingerAsync(int id) => DeleteAsync($"api/singers/{id}");
    public Task<CatalogMutateResult> TryDeleteSingerAsync(int id) => TryDeleteAsync($"api/singers/{id}");

    public Task<List<SongDto>> GetSongsAsync() => GetListAsync<SongDto>("api/songs");
    public Task CreateSongAsync(SongDto dto) => PostAsync("api/songs", dto);
    public Task<CatalogMutateResult> TryCreateSongAsync(SongDto dto) => TryPostAsync("api/songs", dto);
    public Task UpdateSongAsync(SongDto dto) => PutAsync($"api/songs/{dto.Id}", dto);
    public Task<CatalogMutateResult> TryUpdateSongAsync(SongDto dto) => TryPutAsync($"api/songs/{dto.Id}", dto);
    public Task DeleteSongAsync(int id) => DeleteAsync($"api/songs/{id}");
    public Task<CatalogMutateResult> TryDeleteSongAsync(int id) => TryDeleteAsync($"api/songs/{id}");
    public Task<CatalogMutateResult> MergeSongsAsync(int sourceId, int targetId) =>
        TryMutateAsync(() => http.PostAsync($"api/songs/{sourceId}/merge-into/{targetId}", null));

    public async Task<AppVersionDto?> GetAppVersionAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<AppVersionDto>("api/version");
        }
        catch
        {
            return null;
        }
    }

    public async Task<CatalogImportFileResult> ImportCatalogFileAsync(Stream fileStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        try
        {
            var response = await http.PostAsync("api/catalog/import/file", content);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<CatalogImportResultDto>(JsonOptions);
                return body is null
                    ? CatalogImportFileResult.Fail("Unexpected empty response from the server.")
                    : CatalogImportFileResult.Ok(body);
            }
            var message = await ReadApiErrorMessageAsync(response);
            return CatalogImportFileResult.Fail(message ?? "Import failed.");
        }
        catch (Exception ex)
        {
            return CatalogImportFileResult.Fail(ex.Message);
        }
    }

    public async Task<CatalogImportFileResult> ImportCatalogFromGSheetAsync(GSheetImportRequest request)
    {
        var response = await http.PostAsJsonAsync("api/catalog/import/gsheet", request);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<CatalogImportResultDto>(JsonOptions);
            return body is null
                ? CatalogImportFileResult.Fail("Unexpected empty response from the server.")
                : CatalogImportFileResult.Ok(body);
        }
        var message = await ReadApiErrorMessageAsync(response);
        return CatalogImportFileResult.Fail(message ?? "Import from Google Sheets failed.");
    }

    public Task<List<PerformanceDto>> GetPerformancesAsync(int? songId = null)
    {
        var suffix = songId is int song ? $"?songId={song}" : string.Empty;
        return GetListAsync<PerformanceDto>($"api/performances{suffix}");
    }

    public async Task<UserProfileDto?> GetProfileAsync() =>
        await http.GetFromJsonAsync<UserProfileDto>("api/auth/me");

    public async Task<InviteShareDto?> GetInviteShareAsync()
    {
        try
        {
            return await http.GetFromJsonAsync<InviteShareDto>("api/auth/invite-share", JsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public Task<AuthResult> LinkSingerAsync(LinkSingerRequest request) =>
        PostAuthAsync("api/auth/link-singer", request);

    public async Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/change-password", request);
            if (response.IsSuccessStatusCode)
            {
                return ChangePasswordResult.Ok();
            }

            var message = await ReadApiErrorMessageAsync(response);
            return ChangePasswordResult.Fail(message ?? "Could not change password.");
        }
        catch (HttpRequestException ex)
        {
            return ChangePasswordResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return ChangePasswordResult.Fail(ex.Message);
        }
    }

    public Task<PasswordRecoveryResult> ForgotPasswordAsync(ForgotPasswordRequest request) =>
        PostPasswordRecoveryAsync("api/auth/forgot-password", request);

    public Task<PasswordRecoveryResult> ResetPasswordAsync(ResetPasswordRequest request) =>
        PostPasswordRecoveryAsync("api/auth/reset-password", request);

    private async Task<PasswordRecoveryResult> PostPasswordRecoveryAsync(string url, object request)
    {
        try
        {
            var response = await http.PostAsJsonAsync(url, request);
            if (response.IsSuccessStatusCode)
            {
                return PasswordRecoveryResult.Ok();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return PasswordRecoveryResult.Fail("Password recovery is not available.");
            }

            var message = await ReadApiErrorMessageAsync(response);
            return PasswordRecoveryResult.Fail(message ?? "Request failed.");
        }
        catch (HttpRequestException ex)
        {
            return PasswordRecoveryResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return PasswordRecoveryResult.Fail(ex.Message);
        }
    }

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

    public async Task<SingerListsResult> GetMyListsAsync()
    {
        try
        {
            var response = await http.GetAsync("api/singers/me/lists");
            if (response.IsSuccessStatusCode)
            {
                var lists = await response.Content.ReadFromJsonAsync<List<SingerListDto>>(JsonOptions);
                return SingerListsResult.Ok(lists ?? []);
            }

            var message = await ReadApiErrorMessageAsync(response);
            if (string.IsNullOrWhiteSpace(message) && ApiTransientFailure.IsTransient(response.StatusCode))
            {
                message = ApiTransientFailure.ColdStartMessage;
            }

            return SingerListsResult.Fail(message ?? "Could not load singer lists.");
        }
        catch (HttpRequestException ex)
        {
            return SingerListsResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return SingerListsResult.Fail(ApiTransientFailure.ColdStartMessage);
        }
    }

    public async Task<RepertoireResult> GetListSongsAsync(
        int listId,
        string sortBy = "title",
        string sortDir = "asc",
        int? genreId = null)
    {
        var query = $"sortBy={Uri.EscapeDataString(sortBy)}&sortDir={Uri.EscapeDataString(sortDir)}";
        if (genreId is int genre)
        {
            query += $"&genreId={genre}";
        }

        var response = await http.GetAsync($"api/singers/me/lists/{listId}/songs?{query}");
        if (response.IsSuccessStatusCode)
        {
            var songs = await response.Content.ReadFromJsonAsync<List<RepertoireSongDto>>(JsonOptions);
            return RepertoireResult.Ok(songs ?? []);
        }

        var message = await ReadApiErrorMessageAsync(response);
        return RepertoireResult.Fail(message ?? "Could not load list songs.");
    }

    public async Task<SingerListImportResult> ImportListSongsAsync(ImportSingerListSongsRequest request)
    {
        var response = await http.PostAsJsonAsync("api/singers/me/lists/import", request);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ImportSingerListSongsResponse>(JsonOptions);
            return body is null
                ? SingerListImportResult.Fail("Unexpected empty response from the server.")
                : SingerListImportResult.Ok(body);
        }

        var message = await ReadApiErrorMessageAsync(response);
        return SingerListImportResult.Fail(message ?? "Could not import songs.");
    }

    public async Task<SingerListFileImportResult> ImportListSongsFromFileAsync(
        Stream fileStream, string fileName, SingerListKind listKind)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        content.Add(new StringContent(listKind.ToString()), "listKind");
        try
        {
            var response = await http.PostAsync("api/singers/me/lists/import/file", content);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ImportSingerListFromFileResponse>(JsonOptions);
                return body is null
                    ? SingerListFileImportResult.Fail("Unexpected empty response from the server.")
                    : SingerListFileImportResult.Ok(body);
            }

            var partial = await TryReadImportFromFileResponseAsync(response);
            if (partial is not null)
                return SingerListFileImportResult.Fail(await ReadApiErrorMessageAsync(response) ?? "Import failed.", partial);

            var message = await ReadApiErrorMessageAsync(response);
            return SingerListFileImportResult.Fail(message ?? "Import failed.");
        }
        catch (Exception ex)
        {
            return SingerListFileImportResult.Fail(ex.Message);
        }
    }

    public async Task<SingerListFileImportResult> ImportListSongsFromGSheetAsync(
        ImportSingerListFromGSheetRequest request)
    {
        var response = await http.PostAsJsonAsync("api/singers/me/lists/import/gsheet", request);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ImportSingerListFromFileResponse>(JsonOptions);
            return body is null
                ? SingerListFileImportResult.Fail("Unexpected empty response from the server.")
                : SingerListFileImportResult.Ok(body);
        }

        var partial = await TryReadImportFromFileResponseAsync(response);
        if (partial is not null)
            return SingerListFileImportResult.Fail(await ReadApiErrorMessageAsync(response) ?? "Import failed.", partial);

        var message = await ReadApiErrorMessageAsync(response);
        return SingerListFileImportResult.Fail(message ?? "Import from Google Sheets failed.");
    }

    private static async Task<ImportSingerListFromFileResponse?> TryReadImportFromFileResponseAsync(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ImportSingerListFromFileResponse>(JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ListSongActionResult> AddListSongAsync(int listId, int songId)
    {
        var response = await http.PostAsJsonAsync(
            $"api/singers/me/lists/{listId}/songs",
            new AddSingerListSongRequest { SongId = songId });
        if (response.IsSuccessStatusCode)
        {
            return ListSongActionResult.Ok();
        }

        var message = await ReadApiErrorMessageAsync(response);
        return ListSongActionResult.Fail(message ?? "Could not add song to list.");
    }

    public async Task<ListSongActionResult> RemoveListSongAsync(int listId, int songId)
    {
        var response = await http.DeleteAsync($"api/singers/me/lists/{listId}/songs/{songId}");
        if (response.IsSuccessStatusCode)
        {
            return ListSongActionResult.Ok();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return ListSongActionResult.Fail("Song was not on that list.");
        }

        var message = await ReadApiErrorMessageAsync(response);
        return ListSongActionResult.Fail(message ?? "Could not remove song from list.");
    }

    public async Task<SongListMembershipResult> GetSongListMembershipAsync(int songId)
    {
        var response = await http.GetAsync($"api/singers/me/songs/{songId}/list-membership");
        if (response.IsSuccessStatusCode)
        {
            var membership = await response.Content.ReadFromJsonAsync<SongListMembershipDto>(JsonOptions);
            return membership is null
                ? SongListMembershipResult.Fail("Unexpected empty response from the server.")
                : SongListMembershipResult.Ok(membership.Lists);
        }

        var message = await ReadApiErrorMessageAsync(response);
        return SongListMembershipResult.Fail(message ?? "Could not load list membership.");
    }

    public async Task<SongTicklerExclusionResult> GetSongTicklerExclusionAsync(int songId)
    {
        var response = await http.GetAsync($"api/singers/me/songs/{songId}/tickler-exclusion");
        if (response.IsSuccessStatusCode)
        {
            var exclusion = await response.Content.ReadFromJsonAsync<SongTicklerExclusionDto>(JsonOptions);
            return exclusion is null
                ? SongTicklerExclusionResult.Fail("Unexpected empty response from the server.")
                : SongTicklerExclusionResult.Ok(exclusion);
        }

        var message = await ReadApiErrorMessageAsync(response);
        return SongTicklerExclusionResult.Fail(message ?? "Could not load tickler exclusion.");
    }

    public async Task<TicklerExclusionActionResult> SetSongTicklerExclusionAsync(
        int songId,
        UpdateSongTicklerExclusionRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/singers/me/songs/{songId}/tickler-exclusion", request);
        if (response.IsSuccessStatusCode)
        {
            return TicklerExclusionActionResult.Ok();
        }

        var message = await ReadApiErrorMessageAsync(response);
        return TicklerExclusionActionResult.Fail(message ?? "Could not exclude song from tickler.");
    }

    public async Task<TicklerExclusionActionResult> RemoveSongTicklerExclusionAsync(int songId)
    {
        var response = await http.DeleteAsync($"api/singers/me/songs/{songId}/tickler-exclusion");
        if (response.IsSuccessStatusCode)
        {
            return TicklerExclusionActionResult.Ok();
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TicklerExclusionActionResult.Fail("Song was not excluded from the tickler.");
        }

        var message = await ReadApiErrorMessageAsync(response);
        return TicklerExclusionActionResult.Fail(message ?? "Could not include song in tickler again.");
    }

    public async Task<StaleSongsResult> GetMyStaleSongsAsync(int? days = null, int? limit = null)
    {
        try
        {
            var query = new List<string>
            {
                $"asOfDate={PerformanceRelativeDate.ToQueryDate(DateTime.Today)}"
            };
            if (days is int dayValue)
            {
                query.Add($"days={dayValue}");
            }

            if (limit is int limitValue)
            {
                query.Add($"limit={limitValue}");
            }

            var suffix = $"?{string.Join('&', query)}";
            var response = await http.GetAsync($"api/performances/my-stale-songs{suffix}");
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<StaleSongsResponseDto>();
                return payload is null
                    ? StaleSongsResult.Fail("Unexpected empty response from the server.")
                    : StaleSongsResult.Ok(payload);
            }

            var message = await ReadApiErrorMessageAsync(response);
            return StaleSongsResult.Fail(message ?? "Could not load stale songs.");
        }
        catch (HttpRequestException ex)
        {
            return StaleSongsResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return StaleSongsResult.Fail(ex.Message);
        }
    }

    public async Task<TicklerSettingsResult> GetTicklerSettingsAsync()
    {
        try
        {
            var settings = await http.GetFromJsonAsync<TicklerSettingsDto>("api/auth/tickler-settings");
            return settings is null
                ? TicklerSettingsResult.Fail("Unexpected empty response from the server.")
                : TicklerSettingsResult.Ok(settings);
        }
        catch (HttpRequestException ex)
        {
            return TicklerSettingsResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return TicklerSettingsResult.Fail(ex.Message);
        }
    }

    public async Task<TicklerSettingsUpdateResult> UpdateTicklerSettingsAsync(UpdateTicklerSettingsRequest request)
    {
        try
        {
            var response = await http.PutAsJsonAsync("api/auth/tickler-settings", request);
            if (response.IsSuccessStatusCode)
            {
                return TicklerSettingsUpdateResult.Ok();
            }

            var message = await ReadApiErrorMessageAsync(response);
            return TicklerSettingsUpdateResult.Fail(message ?? "Could not save tickler settings.");
        }
        catch (HttpRequestException ex)
        {
            return TicklerSettingsUpdateResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return TicklerSettingsUpdateResult.Fail(ex.Message);
        }
    }

    public async Task<MusicServicePreferenceResult> GetMusicServicePreferenceAsync()
    {
        try
        {
            var preference = await http.GetFromJsonAsync<MusicServicePreferenceDto>("api/auth/music-service");
            return preference is null
                ? MusicServicePreferenceResult.Fail("Unexpected empty response from the server.")
                : MusicServicePreferenceResult.Ok(preference);
        }
        catch (HttpRequestException ex)
        {
            return MusicServicePreferenceResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return MusicServicePreferenceResult.Fail(ex.Message);
        }
    }

    public async Task<MusicServicePreferenceUpdateResult> UpdateMusicServicePreferenceAsync(
        UpdateMusicServicePreferenceRequest request)
    {
        try
        {
            var response = await http.PutAsJsonAsync("api/auth/music-service", request);
            if (response.IsSuccessStatusCode)
            {
                return MusicServicePreferenceUpdateResult.Ok();
            }

            var message = await ReadApiErrorMessageAsync(response);
            return MusicServicePreferenceUpdateResult.Fail(message ?? "Could not save music service preference.");
        }
        catch (HttpRequestException ex)
        {
            return MusicServicePreferenceUpdateResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return MusicServicePreferenceUpdateResult.Fail(ex.Message);
        }
    }

    public async Task<SingerStatsResult> GetMySingerStatsAsync(
        int topVenues = 0,
        int topSongs = 0,
        int topArtists = 0,
        int newRepertoireDays = 0)
    {
        try
        {
            var url =
                $"api/performances/my-stats?topVenues={topVenues}&topSongs={topSongs}&topArtists={topArtists}&newRepertoireDays={newRepertoireDays}&asOfDate={PerformanceRelativeDate.ToQueryDate(DateTime.Today)}";
            var response = await http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var stats = await response.Content.ReadFromJsonAsync<SingerStatsDto>();
                return stats is null
                    ? SingerStatsResult.Fail("Unexpected empty response from the server.")
                    : SingerStatsResult.Ok(stats);
            }

            var message = await ReadApiErrorMessageAsync(response);
            return SingerStatsResult.Fail(message ?? "Could not load stats.");
        }
        catch (HttpRequestException ex)
        {
            return SingerStatsResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex)
        {
            return SingerStatsResult.Fail(ex.Message);
        }
    }

    public async Task<MyPerformancesResult> GetMyPerformancesAsync(int? venueId = null, string sortDir = "desc")
    {
        var query = $"sortDir={Uri.EscapeDataString(sortDir)}";
        if (venueId is int venue)
        {
            query += $"&venueId={venue}";
        }

        try
        {
            var response = await http.GetAsync($"api/performances/my-history?{query}");
            if (response.IsSuccessStatusCode)
            {
                var performances = await response.Content.ReadFromJsonAsync<List<MyPerformanceEntryDto>>();
                return MyPerformancesResult.Ok(performances ?? []);
            }

            var message = await ReadApiErrorMessageAsync(response);
            if (string.IsNullOrWhiteSpace(message) && ApiTransientFailure.IsTransient(response.StatusCode))
            {
                message = ApiTransientFailure.ColdStartMessage;
            }

            return MyPerformancesResult.Fail(message ?? "Could not load performances.");
        }
        catch (HttpRequestException ex)
        {
            return MyPerformancesResult.Fail(
                $"Cannot reach the API at {http.BaseAddress}. Start KaraokeList.Api first. ({ex.Message})");
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return MyPerformancesResult.Fail(ApiTransientFailure.ColdStartMessage);
        }
    }

    public Task CreatePerformanceAsync(PerformanceDto dto) => PostAsync("api/performances", dto);

    public async Task<PerformanceCreateResult> TryCreatePerformanceAsync(PerformanceDto dto)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/performances", dto);
            if (response.IsSuccessStatusCode)
            {
                return new PerformanceCreateResult(true, false, null);
            }

            var message = await ReadApiErrorMessageAsync(response);
            var isTransient = ApiTransientFailure.IsTransient(response.StatusCode);
            return new PerformanceCreateResult(false, isTransient, message ?? "Could not save performance.");
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex))
        {
            return new PerformanceCreateResult(false, true, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return new PerformanceCreateResult(false, true, ex.Message);
        }
    }
    public Task UpdatePerformanceAsync(PerformanceDto dto) => PutAsync($"api/performances/{dto.Id}", dto);

    public Task<CatalogMutateResult> TryUpdatePerformanceAsync(PerformanceDto dto) =>
        TryPutAsync($"api/performances/{dto.Id}", dto);

    public Task DeletePerformanceAsync(int id) => DeleteAsync($"api/performances/{id}");

    public Task<CatalogMutateResult> TryDeletePerformanceAsync(int id) =>
        TryDeleteAsync($"api/performances/{id}");

    public Task<List<AdminUserDto>> GetAdminUsersAsync() =>
        GetListAsync<AdminUserDto>("api/admin/users");

    public async Task<AdminUserUpdateResult> UpdateAdminUserAsync(UpdateAdminUserRequest request)
    {
        try
        {
            var response = await http.PutAsJsonAsync($"api/admin/users/{request.UserId}", request);
            if (response.IsSuccessStatusCode)
            {
                return AdminUserUpdateResult.Ok();
            }

            var message = await ReadApiErrorMessageAsync(response);
            return AdminUserUpdateResult.Fail(message ?? "Could not update user.");
        }
        catch (Exception ex)
        {
            return AdminUserUpdateResult.Fail(ex.Message);
        }
    }

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

    private async Task<CatalogMutateResult> TryPostAsync<T>(string url, T dto) =>
        await TryMutateAsync(() => http.PostAsJsonAsync(url, dto));

    private async Task<CatalogMutateResult> TryPutAsync<T>(string url, T dto) =>
        await TryMutateAsync(() => http.PutAsJsonAsync(url, dto));

    private async Task<CatalogMutateResult> TryDeleteAsync(string url) =>
        await TryMutateAsync(() => http.DeleteAsync(url));

    private async Task<CatalogMutateResult> TryMutateAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            var response = await send();
            if (response.IsSuccessStatusCode)
            {
                return CatalogMutateResult.Ok();
            }

            var message = await ReadApiErrorMessageAsync(response);
            return CatalogMutateResult.Fail(message ?? "Request failed.");
        }
        catch (Exception ex)
        {
            return CatalogMutateResult.Fail(ex.Message);
        }
    }

    public async Task<GenreSuggestionResponse?> SuggestGenreAsync(GenreSuggestionRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/ai/suggest-genre", request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GenreSuggestionResponse>(JsonOptions);
        }
        catch
        {
            return null;
        }
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
