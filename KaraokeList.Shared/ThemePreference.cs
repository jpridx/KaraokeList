using System.ComponentModel.DataAnnotations;

namespace KaraokeList.Shared;

public enum ThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2
}

public class ThemePreferenceDto
{
    public ThemePreference Preference { get; set; } = ThemePreference.System;
}

public class UpdateThemePreferenceRequest
{
    [EnumDataType(typeof(ThemePreference))]
    public ThemePreference Preference { get; set; } = ThemePreference.System;
}

public static class ThemePreferenceCatalog
{
    public static IReadOnlyList<ThemePreference> SelectablePreferences { get; } =
    [
        ThemePreference.System,
        ThemePreference.Light,
        ThemePreference.Dark
    ];

    public static string GetDisplayName(ThemePreference preference) => preference switch
    {
        ThemePreference.System => "System",
        ThemePreference.Light => "Light",
        ThemePreference.Dark => "Dark",
        _ => "System"
    };
}
