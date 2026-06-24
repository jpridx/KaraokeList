using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record TicklerSettingsResult(bool Succeeded, TicklerSettingsDto? Settings, string? ErrorMessage)
{
    public static TicklerSettingsResult Ok(TicklerSettingsDto settings) => new(true, settings, null);
    public static TicklerSettingsResult Fail(string message) => new(false, null, message);
}

public sealed record TicklerSettingsUpdateResult(bool Succeeded, string? ErrorMessage)
{
    public static TicklerSettingsUpdateResult Ok() => new(true, null);
    public static TicklerSettingsUpdateResult Fail(string message) => new(false, message);
}
