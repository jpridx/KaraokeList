using KaraokeList.Shared;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class EditablePerformanceEntryTests
{
    [Fact]
    public void FromBrowse_maps_song_and_performance_fields()
    {
        var entry = EditablePerformanceEntry.FromBrowse(new MyPerformanceEntryDto
        {
            Id = 10,
            SongId = 5,
            Title = "Jeopardy",
            ArtistName = "The Greg Kihn Band",
            PerformedOn = new DateTime(2026, 6, 15),
            VenueId = 2,
            VenueName = "Main Stage",
            KeyChangeSemitones = 1
        });

        Assert.Equal(10, entry.Id);
        Assert.Equal(5, entry.SongId);
        Assert.Equal("Jeopardy", entry.Title);
        Assert.Equal("Main Stage", entry.VenueName);
    }

    [Fact]
    public void FromHistory_uses_page_song_id()
    {
        var entry = EditablePerformanceEntry.FromHistory(new PerformanceHistoryEntryDto
        {
            Id = 3,
            PerformedOn = new DateTime(2026, 5, 1),
            VenueName = "Back Room",
            KeyChangeSemitones = null
        }, songId: 99);

        Assert.Equal(99, entry.SongId);
        Assert.Null(entry.Title);
        Assert.Equal("Back Room", entry.VenueName);
    }
}
