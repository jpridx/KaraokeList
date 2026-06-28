using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class RepertoireSongSortTests
{
    private static readonly DateTime Recent = new(2026, 6, 1);
    private static readonly DateTime Older = new(2025, 1, 15);

    private static readonly RepertoireSongDto RecentSong = new()
    {
        SongId = 1,
        Title = "Zebra",
        ArtistName = "Artist Z",
        LastPerformedOn = Recent,
        PerformanceCount = 2
    };

    private static readonly RepertoireSongDto OlderSong = new()
    {
        SongId = 2,
        Title = "Alpha",
        ArtistName = "Artist A",
        LastPerformedOn = Older,
        PerformanceCount = 1
    };

    private static readonly RepertoireSongDto NeverPerformed = new()
    {
        SongId = 3,
        Title = "Beta",
        ArtistName = "Artist B",
        LastPerformedOn = null,
        PerformanceCount = 0
    };

    private static readonly RepertoireSongDto LadyByStyx = new()
    {
        SongId = 4,
        Title = "Lady",
        ArtistName = "Styx"
    };

    private static readonly RepertoireSongDto LadyByRogers = new()
    {
        SongId = 5,
        Title = "Lady",
        ArtistName = "Kenny Rogers"
    };

    [Fact]
    public void Apply_lastPerformed_desc_sorts_newest_first_and_puts_nulls_last()
    {
        var songs = new[] { NeverPerformed, OlderSong, RecentSong };

        var sorted = RepertoireSongSort.Apply(songs, "lastPerformed", "desc");

        Assert.Equal([RecentSong.SongId, OlderSong.SongId, NeverPerformed.SongId], sorted.Select(s => s.SongId));
    }

    [Fact]
    public void Apply_lastPerformed_asc_puts_nulls_first_as_least_recent()
    {
        var songs = new[] { RecentSong, NeverPerformed, OlderSong };

        var sorted = RepertoireSongSort.Apply(songs, "lastPerformed", "asc");

        Assert.Equal([NeverPerformed.SongId, OlderSong.SongId, RecentSong.SongId], sorted.Select(s => s.SongId));
    }

    [Fact]
    public void Apply_title_uses_artist_as_tiebreaker()
    {
        var songs = new[] { LadyByRogers, LadyByStyx };

        var sorted = RepertoireSongSort.Apply(songs, "title", "asc");

        Assert.Equal([LadyByRogers.SongId, LadyByStyx.SongId], sorted.Select(s => s.SongId));
    }

    [Fact]
    public void Apply_artist_uses_title_as_tiebreaker()
    {
        var artistSongs = new[]
        {
            new RepertoireSongDto { SongId = 10, Title = "Zed", ArtistName = "Same Artist" },
            new RepertoireSongDto { SongId = 11, Title = "Able", ArtistName = "Same Artist" }
        };

        var sorted = RepertoireSongSort.Apply(artistSongs, "artist", "asc");

        Assert.Equal([11, 10], sorted.Select(s => s.SongId));
    }
}
