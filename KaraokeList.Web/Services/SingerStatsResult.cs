using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record SingerStatsResult(bool Succeeded, SingerStatsDto? Stats, string? ErrorMessage)
{
    public static SingerStatsResult Ok(SingerStatsDto stats) => new(true, stats, null);
    public static SingerStatsResult Fail(string message) => new(false, null, message);
}
