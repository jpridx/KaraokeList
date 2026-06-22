using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class LogArtistPickerTests
{
    private static readonly ArtistLookupDto[] Lookups =
    [
        new() { Id = 1, Name = "Neil Diamond" },
        new() { Id = 2, Name = "The Greg Kihn Band" }
    ];

    [Fact]
    public void ResolveArtistId_matches_case_insensitive()
    {
        Assert.Equal(1, LogArtistPicker.ResolveArtistId("neil diamond", Lookups));
    }

    [Fact]
    public void NeedsNewArtist_true_when_name_not_in_catalog()
    {
        Assert.True(LogArtistPicker.NeedsNewArtist("New Artist", Lookups));
    }

    [Fact]
    public void NeedsNewArtist_false_when_name_exists()
    {
        Assert.False(LogArtistPicker.NeedsNewArtist("Neil Diamond", Lookups));
    }

    [Fact]
    public void FindCreatedSong_matches_title_and_artist()
    {
        var songs = new[]
        {
            new SongStub(10, "Sweet Caroline", "Neil Diamond"),
            new SongStub(11, "Jeopardy", "The Greg Kihn Band")
        };

        var match = LogArtistPicker.FindCreatedSong(
            songs,
            "Jeopardy",
            "The Greg Kihn Band",
            s => s.Title,
            s => s.ArtistName);

        Assert.NotNull(match);
        Assert.Equal(11, match.Id);
    }

    private sealed record SongStub(int Id, string Title, string ArtistName);
}
