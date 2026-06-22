using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class MyPerformancesResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public List<MyPerformanceEntryDto> Performances { get; init; } = [];

    public static MyPerformancesResult Ok(List<MyPerformanceEntryDto> performances) =>
        new() { Succeeded = true, Performances = performances };

    public static MyPerformancesResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
