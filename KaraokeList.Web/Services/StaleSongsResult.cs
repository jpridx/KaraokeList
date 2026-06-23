using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record StaleSongsResult(bool Succeeded, StaleSongsResponseDto? Response, string? ErrorMessage)
{
    public static StaleSongsResult Ok(StaleSongsResponseDto response) => new(true, response, null);
    public static StaleSongsResult Fail(string message) => new(false, null, message);
}
