using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class SongTicklerExclusionResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public SongTicklerExclusionDto? Exclusion { get; init; }

    public static SongTicklerExclusionResult Ok(SongTicklerExclusionDto exclusion) =>
        new() { Succeeded = true, Exclusion = exclusion };

    public static SongTicklerExclusionResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public sealed class TicklerExclusionActionResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static TicklerExclusionActionResult Ok() => new() { Succeeded = true };

    public static TicklerExclusionActionResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
