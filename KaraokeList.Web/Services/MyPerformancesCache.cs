using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record CachedMyPerformances(
    IReadOnlyList<MyPerformanceEntryDto> Performances,
    DateTime CachedAtUtc,
    int SchemaVersion = 1);

public sealed record MyPerformancesLoadResult(
    IReadOnlyList<MyPerformanceEntryDto> Performances,
    bool FromCache,
    bool HasCache,
    DateTime? CachedAtUtc,
    string? ErrorMessage,
    bool NeedsSingerLink);
