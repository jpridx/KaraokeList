using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record MusicServicePreferenceResult(bool Succeeded, MusicServicePreferenceDto? Preference, string? ErrorMessage)
{
    public static MusicServicePreferenceResult Ok(MusicServicePreferenceDto preference) => new(true, preference, null);
    public static MusicServicePreferenceResult Fail(string message) => new(false, null, message);
}

public sealed record MusicServicePreferenceUpdateResult(bool Succeeded, string? ErrorMessage)
{
    public static MusicServicePreferenceUpdateResult Ok() => new(true, null);
    public static MusicServicePreferenceUpdateResult Fail(string message) => new(false, message);
}
