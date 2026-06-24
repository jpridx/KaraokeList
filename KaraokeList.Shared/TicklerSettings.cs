using System.ComponentModel.DataAnnotations;

namespace KaraokeList.Shared;

public static class TicklerSettingsLimits
{
    public const int DefaultStaleAfterDays = 90;
    public const int DefaultSongLimit = 5;
    public const int MinStaleAfterDays = 7;
    public const int MaxStaleAfterDays = 365;
    public const int MinSongLimit = 1;
    public const int MaxSongLimit = 20;
}

public class TicklerSettingsDto
{
    public int StaleAfterDays { get; set; } = TicklerSettingsLimits.DefaultStaleAfterDays;
    public int SongLimit { get; set; } = TicklerSettingsLimits.DefaultSongLimit;
}

public class UpdateTicklerSettingsRequest
{
    [Range(TicklerSettingsLimits.MinStaleAfterDays, TicklerSettingsLimits.MaxStaleAfterDays)]
    public int StaleAfterDays { get; set; } = TicklerSettingsLimits.DefaultStaleAfterDays;

    [Range(TicklerSettingsLimits.MinSongLimit, TicklerSettingsLimits.MaxSongLimit)]
    public int SongLimit { get; set; } = TicklerSettingsLimits.DefaultSongLimit;
}
