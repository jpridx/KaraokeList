using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.TestDoubles;

/// <summary>
/// Base <see cref="IKaraokeApiClient"/> test double — every member throws until overridden.
/// Subclass and override only the methods your test needs.
/// </summary>
public class NotImplementedApiClient : IKaraokeApiClient
{
    public virtual Task<AuthResult> LoginAsync(LoginRequest request) => Throw<AuthResult>();
    public virtual Task<AuthResult> RegisterAsync(RegisterRequest request) => Throw<AuthResult>();
    public virtual Task<RegistrationInfoDto?> GetRegistrationInfoAsync() => Throw<RegistrationInfoDto?>();
    public virtual Task<ExternalAuthProvidersDto?> GetExternalAuthProvidersAsync() => Throw<ExternalAuthProvidersDto?>();
    public virtual Task<AuthResult> ExchangeExternalAuthCodeAsync(ExternalAuthExchangeRequest request) => Throw<AuthResult>();
    public virtual Task<List<VenueDto>> GetVenuesAsync() => Throw<List<VenueDto>>();
    public virtual Task CreateVenueAsync(VenueDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryCreateVenueAsync(VenueDto dto) => Throw<CatalogMutateResult>();
    public virtual Task UpdateVenueAsync(VenueDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryUpdateVenueAsync(VenueDto dto) => Throw<CatalogMutateResult>();
    public virtual Task DeleteVenueAsync(int id) => Throw();
    public virtual Task<CatalogMutateResult> TryDeleteVenueAsync(int id) => Throw<CatalogMutateResult>();
    public virtual Task<List<GenreDto>> GetGenresAsync() => Throw<List<GenreDto>>();
    public virtual Task CreateGenreAsync(GenreDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryCreateGenreAsync(GenreDto dto) => Throw<CatalogMutateResult>();
    public virtual Task UpdateGenreAsync(GenreDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryUpdateGenreAsync(GenreDto dto) => Throw<CatalogMutateResult>();
    public virtual Task DeleteGenreAsync(int id) => Throw();
    public virtual Task<CatalogMutateResult> TryDeleteGenreAsync(int id) => Throw<CatalogMutateResult>();
    public virtual Task<List<GenreGroupDto>> GetGenreGroupsAsync() => Throw<List<GenreGroupDto>>();
    public virtual Task<CatalogMutateResult> TryUpdateGenreGroupGenresAsync(int groupId, UpdateGenreGroupGenresRequest request) =>
        Throw<CatalogMutateResult>();
    public virtual Task<List<ArtistDto>> GetArtistsAsync() => Throw<List<ArtistDto>>();
    public virtual Task<List<ArtistLookupDto>> GetArtistLookupsAsync() => Throw<List<ArtistLookupDto>>();
    public virtual Task CreateArtistAsync(ArtistDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryCreateArtistAsync(ArtistDto dto) => Throw<CatalogMutateResult>();
    public virtual Task UpdateArtistAsync(ArtistDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryUpdateArtistAsync(ArtistDto dto) => Throw<CatalogMutateResult>();
    public virtual Task DeleteArtistAsync(int id) => Throw();
    public virtual Task<CatalogMutateResult> TryDeleteArtistAsync(int id) => Throw<CatalogMutateResult>();
    public virtual Task<List<SingerDto>> GetSingersAsync() => Throw<List<SingerDto>>();
    public virtual Task CreateSingerAsync(SingerDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryCreateSingerAsync(SingerDto dto) => Throw<CatalogMutateResult>();
    public virtual Task UpdateSingerAsync(SingerDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryUpdateSingerAsync(SingerDto dto) => Throw<CatalogMutateResult>();
    public virtual Task DeleteSingerAsync(int id) => Throw();
    public virtual Task<CatalogMutateResult> TryDeleteSingerAsync(int id) => Throw<CatalogMutateResult>();
    public virtual Task<List<SongDto>> GetSongsAsync() => Throw<List<SongDto>>();
    public virtual Task<SongDto> CreateSongAsync(SongDto dto) => Throw<SongDto>();
    public virtual Task<CatalogMutateResult> TryCreateSongAsync(SongDto dto) => Throw<CatalogMutateResult>();
    public virtual Task UpdateSongAsync(SongDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryUpdateSongAsync(SongDto dto) => Throw<CatalogMutateResult>();
    public virtual Task DeleteSongAsync(int id) => Throw();
    public virtual Task<CatalogMutateResult> TryDeleteSongAsync(int id) => Throw<CatalogMutateResult>();
    public virtual Task<AppVersionDto?> GetAppVersionAsync() => Throw<AppVersionDto?>();
    public virtual Task<CatalogImportFileResult> ImportCatalogFileAsync(Stream fileStream, string fileName, bool canonicize = true, IProgress<string>? progress = null) => Throw<CatalogImportFileResult>();
    public virtual Task<CatalogImportFileResult> ImportCatalogFromGSheetAsync(GSheetImportRequest request, bool canonicize = true, IProgress<string>? progress = null) => Throw<CatalogImportFileResult>();
    public virtual Task<CatalogMutateResult> MergeSongsAsync(int sourceId, int targetId) => Throw<CatalogMutateResult>();
    public virtual Task<List<PerformanceDto>> GetPerformancesAsync(int? songId = null) => Throw<List<PerformanceDto>>();
    public virtual Task<UserProfileDto?> GetProfileAsync() => Throw<UserProfileDto?>();
    public virtual Task<InviteShareDto?> GetInviteShareAsync() => Throw<InviteShareDto?>();
    public virtual Task<AuthResult> LinkSingerAsync(LinkSingerRequest request) => Throw<AuthResult>();
    public virtual Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordRequest request) => Throw<ChangePasswordResult>();
    public virtual Task<PasswordRecoveryResult> ForgotPasswordAsync(ForgotPasswordRequest request) => Throw<PasswordRecoveryResult>();
    public virtual Task<PasswordRecoveryResult> ResetPasswordAsync(ResetPasswordRequest request) => Throw<PasswordRecoveryResult>();
    public virtual Task<SongSummaryResult> GetMySongSummaryAsync(int songId) => Throw<SongSummaryResult>();
    public virtual Task<SongAboutResult> GetSongAboutAsync(int songId, bool enrich = false) => Throw<SongAboutResult>();
    public virtual Task<RepertoireResult> GetMyRepertoireAsync(
        string sortBy = "lastPerformed",
        string sortDir = "desc",
        int? genreId = null,
        bool includeAll = false) => Throw<RepertoireResult>();
    public virtual Task<RepertoireGenresResult> GetMyRepertoireGenresAsync() => Throw<RepertoireGenresResult>();
    public virtual Task<SingerListsResult> GetMyListsAsync() => Throw<SingerListsResult>();
    public virtual Task<RepertoireResult> GetListSongsAsync(
        int listId,
        string sortBy = "title",
        string sortDir = "asc",
        int? genreId = null) => Throw<RepertoireResult>();
    public virtual Task<SingerListImportResult> ImportListSongsAsync(ImportSingerListSongsRequest request) => Throw<SingerListImportResult>();
    public virtual Task<SingerListFileImportResult> ImportListSongsFromFileAsync(Stream fileStream, string fileName, SingerListKind listKind) => Throw<SingerListFileImportResult>();
    public virtual Task<SingerListFileImportResult> ImportListSongsFromGSheetAsync(ImportSingerListFromGSheetRequest request) => Throw<SingerListFileImportResult>();
    public virtual Task<ListSongActionResult> AddListSongAsync(int listId, int songId) => Throw<ListSongActionResult>();
    public virtual Task<ListSongActionResult> RemoveListSongAsync(int listId, int songId) => Throw<ListSongActionResult>();
    public virtual Task<SongListMembershipResult> GetSongListMembershipAsync(int songId) => Throw<SongListMembershipResult>();
    public virtual Task<SongTicklerExclusionResult> GetSongTicklerExclusionAsync(int songId) => Throw<SongTicklerExclusionResult>();
    public virtual Task<TicklerExclusionActionResult> SetSongTicklerExclusionAsync(int songId, UpdateSongTicklerExclusionRequest request) => Throw<TicklerExclusionActionResult>();
    public virtual Task<TicklerExclusionActionResult> RemoveSongTicklerExclusionAsync(int songId) => Throw<TicklerExclusionActionResult>();
    public virtual Task<SongGenreUpdateResult> UpdateSongGenreAsync(int songId, UpdateSongGenreRequest request) => Throw<SongGenreUpdateResult>();
    public virtual Task<StaleSongsResult> GetMyStaleSongsAsync(int? days = null, int? limit = null) => Throw<StaleSongsResult>();
    public virtual Task<TicklerSettingsResult> GetTicklerSettingsAsync() => Throw<TicklerSettingsResult>();
    public virtual Task<TicklerSettingsUpdateResult> UpdateTicklerSettingsAsync(UpdateTicklerSettingsRequest request) => Throw<TicklerSettingsUpdateResult>();
    public virtual Task<MusicServicePreferenceResult> GetMusicServicePreferenceAsync() => Throw<MusicServicePreferenceResult>();
    public virtual Task<MusicServicePreferenceUpdateResult> UpdateMusicServicePreferenceAsync(UpdateMusicServicePreferenceRequest request) => Throw<MusicServicePreferenceUpdateResult>();
    public virtual Task<ThemePreferenceResult> GetThemePreferenceAsync() => Throw<ThemePreferenceResult>();
    public virtual Task<ThemePreferenceUpdateResult> UpdateThemePreferenceAsync(UpdateThemePreferenceRequest request) => Throw<ThemePreferenceUpdateResult>();
    public virtual Task<SingerStatsResult> GetMySingerStatsAsync(
        int topVenues = 0,
        int topSongs = 0,
        int topArtists = 0,
        int newRepertoireDays = 0) => Throw<SingerStatsResult>();
    public virtual Task<MyPerformancesResult> GetMyPerformancesAsync(int? venueId = null, string sortDir = "desc") => Throw<MyPerformancesResult>();
    public virtual Task CreatePerformanceAsync(PerformanceDto dto) => Throw();
    public virtual Task<PerformanceCreateResult> TryCreatePerformanceAsync(PerformanceDto dto) => Throw<PerformanceCreateResult>();
    public virtual Task UpdatePerformanceAsync(PerformanceDto dto) => Throw();
    public virtual Task<CatalogMutateResult> TryUpdatePerformanceAsync(PerformanceDto dto) => Throw<CatalogMutateResult>();
    public virtual Task DeletePerformanceAsync(int id) => Throw();
    public virtual Task<CatalogMutateResult> TryDeletePerformanceAsync(int id) => Throw<CatalogMutateResult>();
    public virtual Task<List<AdminUserDto>> GetAdminUsersAsync() => Throw<List<AdminUserDto>>();
    public virtual Task<AdminUserUpdateResult> UpdateAdminUserAsync(UpdateAdminUserRequest request) => Throw<AdminUserUpdateResult>();
    public virtual Task<GenreSuggestionResponse?> SuggestGenreAsync(GenreSuggestionRequest request) => Throw<GenreSuggestionResponse?>();
    public virtual Task<CanonicalLookupResponse?> LookupCanonicalAsync(CanonicalLookupRequest request) => Throw<CanonicalLookupResponse?>();
    public virtual Task<ApplyCanonicalResponse?> ApplyCanonicalAsync(ApplyCanonicalRequest request) => Throw<ApplyCanonicalResponse?>();
    public virtual Task<CatalogVerifyResultDto?> VerifyCatalogAsync(CatalogVerifyRequest request) => Throw<CatalogVerifyResultDto?>();
    public virtual Task<CatalogClearMatchesResultDto?> ClearCanonicalMatchesAsync(CatalogClearMatchesRequest request) => Throw<CatalogClearMatchesResultDto?>();

    private static Task Throw() =>
        throw new NotImplementedException("Override this IKaraokeApiClient member in your test stub.");

    private static Task<T> Throw<T>() =>
        throw new NotImplementedException("Override this IKaraokeApiClient member in your test stub.");
}
