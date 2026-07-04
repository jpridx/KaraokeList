using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record CachedStaleSongs(
    StaleSongsResponseDto Response,
    DateTime CachedAtUtc);

public sealed record StaleSongsLoadResult(
    bool Succeeded,
    bool FromCache,
    StaleSongsResponseDto? Response,
    DateTime? CachedAtUtc,
    string? ErrorMessage)
{
    public static StaleSongsLoadResult Live(StaleSongsResponseDto response) =>
        new(true, false, response, null, null);

    public static StaleSongsLoadResult Cached(StaleSongsResponseDto response, DateTime cachedAtUtc) =>
        new(true, true, response, cachedAtUtc, null);

    public static StaleSongsLoadResult Failed(string errorMessage) =>
        new(false, false, null, null, errorMessage);
}
