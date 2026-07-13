namespace KaraokeList.Web.Components;

public sealed record CanonicalAppliedEventArgs(
    string Title,
    string ArtistName,
    string? RecordingMbid,
    string? ArtistMbid,
    string ArtistCreditDisplay = "",
    IReadOnlyList<string>? ArtistNames = null,
    int? Year = null,
    string? SuggestedGenreName = null);
