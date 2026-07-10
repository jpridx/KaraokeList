using KaraokeList.Data;
using KaraokeList.Shared;

namespace KaraokeList.Api.Services;

public static class ThemePreferenceResolver
{
    public static ThemePreferenceDto ToDto(ApplicationUser user) => new()
    {
        Preference = user.ThemePreference
    };

    public static string? Validate(ThemePreference preference) =>
        Enum.IsDefined(preference) ? null : "Invalid theme preference.";
}
