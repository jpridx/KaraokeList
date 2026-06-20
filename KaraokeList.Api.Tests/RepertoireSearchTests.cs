using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class RepertoireSearchTests
{
    private static readonly RepertoireSongDto KennyLoggins = new()
    {
        SongId = 1,
        Title = "Footloose",
        ArtistName = "Kenny Loggins",
        GenreName = "Pop"
    };

    private static readonly RepertoireSongDto AnotherSong = new()
    {
        SongId = 2,
        Title = "Don't Stop Believin'",
        ArtistName = "Journey",
        GenreName = "Rock"
    };

    private static readonly RepertoireSongDto[] Catalog = [KennyLoggins, AnotherSong];

    [Theory]
    [InlineData("Loggins ")]
    [InlineData(" Loggins")]
    [InlineData("  Loggins  ")]
    [InlineData("loggins")]
    public void Filter_trims_search_text_before_matching_artist(string searchText)
    {
        var results = RepertoireSearch.Filter(Catalog, searchText).ToList();

        Assert.Single(results);
        Assert.Equal(KennyLoggins.SongId, results[0].SongId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Filter_returns_all_songs_when_search_is_blank(string? searchText)
    {
        var results = RepertoireSearch.Filter(Catalog, searchText).ToList();

        Assert.Equal(Catalog.Length, results.Count);
    }

    [Fact]
    public void Filter_matches_title_genre_and_artist_case_insensitively()
    {
        Assert.Single(RepertoireSearch.Filter(Catalog, "footloose"));
        Assert.Single(RepertoireSearch.Filter(Catalog, "POP "));
        Assert.Single(RepertoireSearch.Filter(Catalog, " journey"));
    }

    [Fact]
    public void Filter_returns_empty_when_no_song_matches()
    {
        Assert.Empty(RepertoireSearch.Filter(Catalog, "zzzz "));
    }
}
