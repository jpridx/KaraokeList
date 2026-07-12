namespace KaraokeList.Web.Components;

public sealed record CanonicalAppliedEventArgs(
    string Title,
    string ArtistName,
    string? RecordingMbid,
    string? ArtistMbid);
