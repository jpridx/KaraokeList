using KaraokeList.Shared;

namespace KaraokeList.Web.Tests;

public sealed class StaleSongsComputerTests
{
    private static readonly DateTime Today = new(2026, 6, 15);

    [Fact]
    public void GetCandidates_includes_stale_and_unperformed_repertoire_songs()
    {
        var repertoire = new List<RepertoireSongDto>
        {
            new()
            {
                SongId = 1,
                Title = "Stale",
                ArtistName = "A",
                LastPerformedOn = Today.AddDays(-120),
                PerformanceCount = 2
            },
            new()
            {
                SongId = 2,
                Title = "Fresh",
                ArtistName = "B",
                LastPerformedOn = Today.AddDays(-10),
                PerformanceCount = 1
            },
            new()
            {
                SongId = 3,
                Title = "Never",
                ArtistName = "C",
                PerformanceCount = 0
            }
        };

        var candidates = StaleSongsComputer.GetCandidates(
            repertoire,
            excludedSongIds: new HashSet<int>(),
            new TicklerSettingsDto { StaleAfterDays = 90, SongLimit = 5 },
            Today);

        Assert.Contains(candidates, s => s.SongId == 1);
        Assert.Contains(candidates, s => s.SongId == 3);
        Assert.DoesNotContain(candidates, s => s.SongId == 2);
    }

    [Fact]
    public void GetCandidates_omits_excluded_songs()
    {
        var repertoire = new List<RepertoireSongDto>
        {
            new()
            {
                SongId = 1,
                Title = "Stale",
                ArtistName = "A",
                LastPerformedOn = Today.AddDays(-120),
                PerformanceCount = 1
            }
        };

        var candidates = StaleSongsComputer.GetCandidates(
            repertoire,
            excludedSongIds: new HashSet<int> { 1 },
            new TicklerSettingsDto { StaleAfterDays = 90, SongLimit = 5 },
            Today);

        Assert.Empty(candidates);
    }

    [Fact]
    public void Compute_respects_song_limit_with_seeded_random()
    {
        var repertoire = Enumerable.Range(1, 10)
            .Select(id => new RepertoireSongDto
            {
                SongId = id,
                Title = $"Song {id}",
                ArtistName = "Artist",
                PerformanceCount = 0
            })
            .ToList();

        var response = StaleSongsComputer.Compute(
            repertoire,
            excludedSongIds: new HashSet<int>(),
            new TicklerSettingsDto { StaleAfterDays = 90, SongLimit = 3 },
            Today,
            new Random(42));

        Assert.Equal(3, response.Songs.Count);
        Assert.Equal(90, response.StaleAfterDays);
    }

    [Fact]
    public void GetCandidates_excludes_song_logged_today_after_patch_scenario()
    {
        var repertoire = new List<RepertoireSongDto>
        {
            new()
            {
                SongId = 42,
                Title = "Footloose",
                ArtistName = "Kenny Loggins",
                LastPerformedOn = Today,
                PerformanceCount = 4
            }
        };

        var candidates = StaleSongsComputer.GetCandidates(
            repertoire,
            excludedSongIds: new HashSet<int>(),
            new TicklerSettingsDto { StaleAfterDays = 90, SongLimit = 5 },
            Today);

        Assert.DoesNotContain(candidates, s => s.SongId == 42);
    }
}
