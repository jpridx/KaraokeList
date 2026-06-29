using KaraokeList.Shared;

namespace KaraokeList.Web.Components;

public enum EditablePerformanceListVariant
{
    Browse,
    History
}

public sealed record EditablePerformanceEntry(
    int Id,
    int SongId,
    string? Title,
    string? ArtistName,
    DateTime PerformedOn,
    int? VenueId,
    string VenueName,
    int? KeyChangeSemitones,
    IReadOnlyList<CoPerformerDto> OtherPerformers)
{
    public static EditablePerformanceEntry FromBrowse(MyPerformanceEntryDto entry) =>
        new(
            entry.Id,
            entry.SongId,
            entry.Title,
            entry.ArtistName,
            entry.PerformedOn,
            entry.VenueId,
            entry.VenueName,
            entry.KeyChangeSemitones,
            entry.OtherPerformers);

    public static EditablePerformanceEntry FromHistory(PerformanceHistoryEntryDto entry, int songId) =>
        new(
            entry.Id,
            songId,
            null,
            null,
            entry.PerformedOn,
            entry.VenueId,
            entry.VenueName,
            entry.KeyChangeSemitones,
            entry.OtherPerformers);
}
