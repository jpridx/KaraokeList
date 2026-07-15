using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class SongAboutResult
{
    public bool Succeeded { get; init; }
    public SongAboutDto? About { get; init; }
    public string? ErrorMessage { get; init; }

    public static SongAboutResult Ok(SongAboutDto about) =>
        new() { Succeeded = true, About = about };

    public static SongAboutResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
