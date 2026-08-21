namespace KaraokeList.Web.Components;

public sealed record PerformanceSavedEventArgs(
    bool SavedOnServer,
    string? Message,
    DateTime PerformedOn);
