using KaraokeList.Api.Services;
using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class MusicBrainzSearchHelperTests
{
    [Theory]
    [InlineData("Rockin' Pneumonia", "Rockin Pneumonia")]
    [InlineData("Don't Stop", "Dont Stop")]
    public void WithoutApostrophes_removes_apostrophes(string input, string expected) =>
        Assert.Equal(expected, MusicBrainzSearchHelper.WithoutApostrophes(input));

    [Theory]
    [InlineData("Rockin' Pneumonia (Live)", "Rockin' Pneumonia")]
    [InlineData("Jeopardy [Remaster]", "Jeopardy")]
    public void NormalizeSearchTerm_strips_parentheticals(string input, string expected) =>
        Assert.Equal(expected, MusicBrainzSearchHelper.NormalizeSearchTerm(input));

    [Theory]
    [InlineData("The Greg Kihn Band", "Greg Kihn Band")]
    [InlineData("Greg Kihn Band", "Greg Kihn Band")]
    public void StripLeadingArticle_removes_leading_the(string input, string expected) =>
        Assert.Equal(expected, MusicBrainzSearchHelper.StripLeadingArticle(input));

    [Fact]
    public void BuildSearchQueries_includes_strict_and_relaxed_variants()
    {
        var queries = MusicBrainzSearchHelper.BuildSearchQueries("My Sharona", "The Knack");

        Assert.Contains("\"My Sharona\" AND artist:\"The Knack\"", queries);
        Assert.Contains("My Sharona AND artist:\"The Knack\"", queries);
        Assert.Contains("\"My Sharona\" AND artist:\"Knack\"", queries);
    }

    [Fact]
    public void ResolveEarliestReleaseYear_returns_minimum_year()
    {
        var year = MusicBrainzSearchHelper.ResolveEarliestReleaseYear(["1994", "1979-08", "1982"]);

        Assert.Equal(1979, year);
    }

    [Fact]
    public void SortMatchesOldestFirst_orders_by_year_ascending()
    {
        var sorted = MusicBrainzSearchHelper.SortMatchesOldestFirst(
        [
            new CanonicalMatchDto { Title = "Reissue", Year = 1994, Score = 100 },
            new CanonicalMatchDto { Title = "Original", Year = 1979, Score = 95 },
            new CanonicalMatchDto { Title = "Undated", Year = null, Score = 99 }
        ]);

        Assert.Equal("Original", sorted[0].Title);
        Assert.Equal("Reissue", sorted[1].Title);
        Assert.Equal("Undated", sorted[2].Title);
    }

    [Fact]
    public void SortMatchesOldestFirst_puts_likely_reissues_last()
    {
        var sorted = MusicBrainzSearchHelper.SortMatchesOldestFirst(
        [
            new CanonicalMatchDto { Title = "Compilation", Year = 1979, Score = 100, Disambiguation = "1994 compilation" },
            new CanonicalMatchDto { Title = "Single", Year = 1979, Score = 90 }
        ]);

        Assert.Equal("Single", sorted[0].Title);
        Assert.Equal("Compilation", sorted[1].Title);
    }
}

public class MusicBrainzRecordingSelectionTests
{
    [Fact]
    public void SelectHeadOfClassRecording_prefers_earliest_release_over_higher_score()
    {
        var recordings = new List<MusicBrainzService.MusicBrainzRecording>
        {
            new()
            {
                Id = "reissue",
                Score = 100,
                FirstReleaseDate = "1994",
                Title = "My Sharona"
            },
            new()
            {
                Id = "original",
                Score = 95,
                FirstReleaseDate = "1979",
                Title = "My Sharona"
            }
        };

        var selected = MusicBrainzService.SelectHeadOfClassRecording(recordings);

        Assert.Equal("original", selected?.Id);
    }

    [Fact]
    public void SelectHeadOfClassRecording_deprioritizes_live_recordings()
    {
        var recordings = new List<MusicBrainzService.MusicBrainzRecording>
        {
            new()
            {
                Id = "live",
                Score = 100,
                FirstReleaseDate = "1979",
                Disambiguation = "live",
                Title = "Jeopardy"
            },
            new()
            {
                Id = "studio",
                Score = 90,
                FirstReleaseDate = "1981",
                Title = "Jeopardy"
            }
        };

        var selected = MusicBrainzService.SelectHeadOfClassRecording(recordings);

        Assert.Equal("studio", selected?.Id);
    }

    [Fact]
    public void GetEarliestReleaseYear_uses_release_group_and_release_dates()
    {
        var recording = new MusicBrainzService.MusicBrainzRecording
        {
            FirstReleaseDate = "2005",
            ReleaseGroups =
            [
                new MusicBrainzService.MusicBrainzReleaseGroup { FirstReleaseDate = "2008-03" }
            ],
            Releases =
            [
                new MusicBrainzService.MusicBrainzRelease { Date = "2007" }
            ]
        };

        var year = MusicBrainzService.GetEarliestReleaseYear(recording);

        Assert.Equal(2005, year);
    }

    [Fact]
    public void OrderMatchesOldestFirst_promotes_earliest_studio_recording()
    {
        var recordingsById = new Dictionary<string, MusicBrainzService.MusicBrainzRecording>(StringComparer.Ordinal)
        {
            ["1994"] = new() { Id = "1994", FirstReleaseDate = "1994", Score = 100 },
            ["1979"] = new() { Id = "1979", FirstReleaseDate = "1979", Score = 95 }
        };

        var ordered = MusicBrainzService.OrderMatchesOldestFirst(
        [
            new CanonicalMatchDto { RecordingMbid = "1994", Title = "My Sharona", Year = 1994, Score = 100 },
            new CanonicalMatchDto { RecordingMbid = "1979", Title = "My Sharona", Year = 1979, Score = 95 }
        ],
        recordingsById);

        Assert.Equal("1979", ordered[0].RecordingMbid);
        Assert.Equal("1994", ordered[1].RecordingMbid);
    }
}
