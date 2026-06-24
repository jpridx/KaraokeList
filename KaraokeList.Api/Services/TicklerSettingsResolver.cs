using KaraokeList.Data;
using KaraokeList.Shared;

namespace KaraokeList.Api.Services;

public static class TicklerSettingsResolver
{
    public static TicklerSettingsDto ToDto(ApplicationUser user) => new()
    {
        StaleAfterDays = user.StaleSongAfterDays,
        SongLimit = user.StaleSongLimit
    };

    public static (int Days, int Limit, string? Error) Resolve(ApplicationUser? user, int? days, int? limit)
    {
        var effectiveDays = days ?? user?.StaleSongAfterDays ?? TicklerSettingsLimits.DefaultStaleAfterDays;
        var effectiveLimit = limit ?? user?.StaleSongLimit ?? TicklerSettingsLimits.DefaultSongLimit;

        if (effectiveDays is < TicklerSettingsLimits.MinStaleAfterDays or > TicklerSettingsLimits.MaxStaleAfterDays)
        {
            return (0, 0, $"Invalid days. Use a value between {TicklerSettingsLimits.MinStaleAfterDays} and {TicklerSettingsLimits.MaxStaleAfterDays}.");
        }

        if (effectiveLimit is < TicklerSettingsLimits.MinSongLimit or > TicklerSettingsLimits.MaxSongLimit)
        {
            return (0, 0, $"Invalid limit. Use a value between {TicklerSettingsLimits.MinSongLimit} and {TicklerSettingsLimits.MaxSongLimit}.");
        }

        return (effectiveDays, effectiveLimit, null);
    }
}
