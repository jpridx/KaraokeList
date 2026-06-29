using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class SongSummaryHints
{
    public static string Format(SongPerformanceSummaryDto summary) =>
        summary.PerformanceCount > 0
            ? $"You've sung this {summary.PerformanceCount} time(s). Last: {summary.LastPerformedOn:d} · {KeyChangeFormatting.Describe(summary.LastKeyChangeSemitones)}"
            : "First time logging this song for you.";

    public static async Task<string?> LoadForSongAsync(IKaraokeApiClient api, int songId)
    {
        try
        {
            var result = await api.GetMySongSummaryAsync(songId);
            if (!result.Succeeded || result.Summary is null)
            {
                return "Ready to log.";
            }

            return Format(result.Summary);
        }
        catch (Exception ex) when (ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException)
        {
            return "Ready to log (offline).";
        }
    }
}
