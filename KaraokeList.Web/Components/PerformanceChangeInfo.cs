using KaraokeList.Web.Services;

namespace KaraokeList.Web.Components;

/// <summary>
/// Raised after a performance is updated or deleted from an editable list.
/// </summary>
public sealed record PerformanceChangeInfo(
    PerformanceEditSnapshot? Updated,
    int? DeletedPerformanceId);
