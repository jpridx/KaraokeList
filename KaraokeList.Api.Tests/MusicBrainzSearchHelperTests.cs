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

    [Theory]
    [InlineData("Queen feat. David Bowie", "Queen")]
    [InlineData("Kenny Rogers ft. Dolly Parton", "Kenny Rogers")]
    public void StripFeaturingSuffix_removes_featuring_clause(string input, string expected) =>
        Assert.Equal(expected, MusicBrainzSearchHelper.StripFeaturingSuffix(input));

    [Theory]
    [InlineData("Don't Stop Believin'", "Don't Stop Believin'", true)]
    [InlineData("Dont Stop Believin", "Don't Stop Believin'", true)]
    [InlineData("Other Song", "Don't Stop Believin'", false)]
    public void TitleMatchesSearch_ignores_punctuation_and_apostrophes(
        string recordingTitle,
        string searchTitle,
        bool expected) =>
        Assert.Equal(expected, MusicBrainzSearchHelper.TitleMatchesSearch(recordingTitle, searchTitle));

    [Fact]
    public void NamesMatchCatalog_treats_apostrophe_variants_as_equal()
    {
        var match = new CanonicalMatchDto
        {
            Title = "Don't Stop Believin'",
            ArtistCreditDisplay = "Journey"
        };

        Assert.True(MusicBrainzSearchHelper.NamesMatchCatalog("Dont Stop Believin", "Journey", match));
    }

    [Fact]
    public void BuildSearchQueries_includes_strict_and_relaxed_variants()
    {
        var queries = MusicBrainzSearchHelper.BuildSearchQueries("My Sharona", "The Knack");

        Assert.Contains("\"My Sharona\" AND artist:\"The Knack\"", queries);
        Assert.Contains("My Sharona AND artist:\"The Knack\"", queries);
        Assert.Contains("My Sharona AND artist:The Knack", queries);
        Assert.Contains("\"My Sharona\" AND artist:\"Knack\"", queries);
    }

    [Fact]
    public void BuildSearchQueries_includes_hyphen_and_and_variants()
    {
        var queries = MusicBrainzSearchHelper.BuildSearchQueries("Semi-Charmed Life", "Third Eye Blind");

        Assert.Contains(queries, q => q.Contains("Semi Charmed Life", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveEarliestReleaseYear_returns_minimum_year()
    {
        var year = MusicBrainzSearchHelper.ResolveEarliestReleaseYear(["1994", "1979-08", "1982"]);

        Assert.Equal(1979, year);
    }

    [Fact]
    public void RankMatches_orders_by_year_ascending()
    {
        var sorted = MusicBrainzSearchHelper.RankMatches(
        [
            new CanonicalMatchDto { Title = "Reissue", Year = 1994, Score = 100 },
            new CanonicalMatchDto { Title = "Original", Year = 1979, Score = 95 },
            new CanonicalMatchDto { Title = "Undated", Year = null, Score = 99 }
        ],
        "Original");

        Assert.Equal("Original", sorted[0].Title);
        Assert.Equal("Reissue", sorted[1].Title);
        Assert.Equal("Undated", sorted[2].Title);
    }

    [Fact]
    public void RankMatches_puts_dj_mix_last()
    {
        var sorted = MusicBrainzSearchHelper.RankMatches(
        [
            new CanonicalMatchDto
            {
                Title = "My Sharona",
                Year = 2007,
                Score = 100,
                Disambiguation = "part of Mastermix: Flashback 1979–1983 DJ-mix"
            },
            new CanonicalMatchDto { Title = "My Sharona", Year = 1979, Score = 100 }
        ],
        "My Sharona");

        Assert.Equal(1979, sorted[0].Year);
        Assert.Equal(2007, sorted[1].Year);
    }

    [Fact]
    public void RankMatches_keeps_oldest_exact_title_despite_compilation_tag()
    {
        var sorted = MusicBrainzSearchHelper.RankMatches(
        [
            new CanonicalMatchDto { Title = "Coward of the County", Year = 1989, Score = 100 },
            new CanonicalMatchDto { Title = "Coward of the County", Year = 1979, Score = 100 }
        ],
        "Coward of the County",
        match => match.Year == 1979);

        Assert.Equal(1979, sorted[0].Year);
        Assert.Equal(1989, sorted[1].Year);
    }

    [Fact]
    public void BuildStudioSearchQueries_excludes_live_recordings()
    {
        var queries = MusicBrainzSearchHelper.BuildStudioSearchQueries("Strutter", "KISS");

        Assert.Equal(2, queries.Count);
        Assert.All(queries, q => Assert.Contains("NOT live", q, StringComparison.Ordinal));
        Assert.Contains(queries, q => q.Contains("artist:KISS", StringComparison.Ordinal));
    }

    [Fact]
    public void RankMatches_promotes_studio_strutter_over_live_and_demo()
    {
        var sorted = MusicBrainzSearchHelper.RankMatches(
        [
            new CanonicalMatchDto
            {
                Title = "Strutter",
                Year = 1984,
                Score = 100,
                Disambiguation = "live, 1984-11-04: IJsselhallen, Zwolle, NL"
            },
            new CanonicalMatchDto
            {
                Title = "Strutter",
                Year = 1970,
                Score = 100,
                Disambiguation = "demo"
            },
            new CanonicalMatchDto { Title = "Strutter", Year = 1974, Score = 100 }
        ],
        "Strutter");

        Assert.Equal(1974, sorted[0].Year);
    }

    [Fact]
    public void SelectBestCredibleSuggestion_promotes_1974_strutter_over_live_2014()
    {
        var pool = new[]
        {
            new CanonicalMatchDto
            {
                Found = true,
                Title = "Strutter",
                Year = 2014,
                Score = 100,
                RecordingMbid = "live-2014",
                Disambiguation = "live, 2014-11-04: IJsselhallen, Zwolle, NL"
            },
            new CanonicalMatchDto
            {
                Found = true,
                Title = "Strutter",
                Year = 1970,
                Score = 100,
                RecordingMbid = "demo",
                Disambiguation = "demo"
            },
            new CanonicalMatchDto
            {
                Found = true,
                Title = "Strutter",
                Year = 1974,
                Score = 95,
                RecordingMbid = "studio-1974"
            }
        };

        var best = MusicBrainzSearchHelper.SelectBestCredibleSuggestion(pool, "Strutter");

        Assert.Equal("studio-1974", best?.RecordingMbid);
        Assert.Equal(1974, best?.Year);
    }

    [Fact]
    public void IsBestCredibleMatch_is_false_when_primary_is_not_oldest_credible_hit()
    {
        var pool = new[]
        {
            new CanonicalMatchDto { Found = true, Title = "Strutter", Year = 2014, RecordingMbid = "live-2014", Disambiguation = "live" },
            new CanonicalMatchDto { Found = true, Title = "Strutter", Year = 1974, RecordingMbid = "studio-1974" }
        };

        Assert.False(MusicBrainzSearchHelper.IsBestCredibleMatch(pool[0], "Strutter", pool));
        Assert.True(MusicBrainzSearchHelper.IsBestCredibleMatch(pool[1], "Strutter", pool));
    }

    [Fact]
    public void SortMatchesOldestFirst_puts_likely_reissues_last()
    {
        var sorted = MusicBrainzSearchHelper.SortMatchesOldestFirst(
        [
            new CanonicalMatchDto { Title = "Compilation", Year = 1979, Score = 100, Disambiguation = "1994 compilation" },
            new CanonicalMatchDto { Title = "Single", Year = 1979, Score = 90 }
        ],
        "Single");

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

        var selected = MusicBrainzService.SelectHeadOfClassRecording(recordings, "My Sharona");

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

        var selected = MusicBrainzService.SelectHeadOfClassRecording(recordings, "Jeopardy");

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
    public void RankMatches_promotes_earliest_exact_title_recording()
    {
        var recordingsById = new Dictionary<string, MusicBrainzService.MusicBrainzRecording>(StringComparer.Ordinal)
        {
            ["1994"] = new() { Id = "1994", FirstReleaseDate = "1994", Score = 100, Title = "My Sharona" },
            ["1979"] = new() { Id = "1979", FirstReleaseDate = "1979", Score = 95, Title = "My Sharona" }
        };

        var ordered = MusicBrainzService.RankMatches(
        [
            new CanonicalMatchDto { RecordingMbid = "1994", Title = "My Sharona", Year = 1994, Score = 100 },
            new CanonicalMatchDto { RecordingMbid = "1979", Title = "My Sharona", Year = 1979, Score = 95 }
        ],
        "My Sharona",
        recordingsById);

        Assert.Equal("1979", ordered[0].RecordingMbid);
        Assert.Equal("1994", ordered[1].RecordingMbid);
    }

    [Fact]
    public void RankMatches_promotes_1979_coward_over_1989_despite_compilation_metadata()
    {
        var recordingsById = new Dictionary<string, MusicBrainzService.MusicBrainzRecording>(StringComparer.Ordinal)
        {
            ["1989"] = new()
            {
                Id = "1989",
                FirstReleaseDate = "1989",
                Score = 100,
                Title = "Coward of the County"
            },
            ["1979"] = new()
            {
                Id = "1979",
                FirstReleaseDate = "1979",
                Score = 100,
                Title = "Coward of the County",
                ReleaseGroups =
                [
                    new MusicBrainzService.MusicBrainzReleaseGroup
                    {
                        SecondaryTypes = ["Compilation"]
                    }
                ]
            }
        };

        var ordered = MusicBrainzService.RankMatches(
        [
            new CanonicalMatchDto { RecordingMbid = "1989", Title = "Coward of the County", Year = 1989, Score = 100 },
            new CanonicalMatchDto { RecordingMbid = "1979", Title = "Coward of the County", Year = 1979, Score = 100 }
        ],
        "Coward of the County",
        recordingsById);

        Assert.Equal("1979", ordered[0].RecordingMbid);
    }
}
