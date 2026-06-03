using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class SongSummaryResult
{
    public bool Succeeded { get; init; }
    public SongPerformanceSummaryDto? Summary { get; init; }
    public string? ErrorMessage { get; init; }

    public static SongSummaryResult Ok(SongPerformanceSummaryDto summary) =>
        new() { Succeeded = true, Summary = summary };

    public static SongSummaryResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
