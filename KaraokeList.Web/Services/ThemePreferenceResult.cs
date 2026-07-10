using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record ThemePreferenceResult(bool Succeeded, ThemePreferenceDto? Preference, string? ErrorMessage)
{
    public static ThemePreferenceResult Ok(ThemePreferenceDto preference) => new(true, preference, null);
    public static ThemePreferenceResult Fail(string message) => new(false, null, message);
}

public sealed record ThemePreferenceUpdateResult(bool Succeeded, string? ErrorMessage)
{
    public static ThemePreferenceUpdateResult Ok() => new(true, null);
    public static ThemePreferenceUpdateResult Fail(string message) => new(false, message);
}
