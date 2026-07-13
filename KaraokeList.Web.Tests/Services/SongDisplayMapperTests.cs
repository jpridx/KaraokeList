using KaraokeList.Shared;
using KaraokeList.Web.Models;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class SongDisplayMapperTests
{
    [Fact]
    public void ToDisplay_maps_artist_and_genre_names()
    {
        var song = new SongDto
        {
            Id = 1,
            Title = "Jeopardy",
            Genre = 20,
            ArtistCreditDisplay = "The Greg Kihn Band feat. Guest",
            Artists =
            [
                new SongArtistDto { ArtistId = 10, DisplayOrder = 0, Name = "The Greg Kihn Band" },
                new SongArtistDto { ArtistId = 11, DisplayOrder = 1, Name = "Guest" }
            ]
        };

        var display = SongDisplayMapper.ToDisplay(
            song,
            [new ArtistLookupDto { Id = 10, Name = "The Greg Kihn Band" }, new ArtistLookupDto { Id = 11, Name = "Guest" }],
            [new GenreDto { Id = 20, GenreName = "Rock" }]);

        Assert.Equal("The Greg Kihn Band feat. Guest", display.ArtistDisplay);
        Assert.Equal("Rock", display.GenreName);
        Assert.Equal(2, display.Artists.Count);
    }

    [Fact]
    public void ApplyArtistLookups_sets_foreign_keys_from_names()
    {
        var display = new SongDisplay
        {
            Artists =
            [
                new SongArtistDto { DisplayOrder = 0, Name = "Neil Diamond" }
            ]
        };

        SongDisplayMapper.ApplyArtistLookups(
            display,
            [new ArtistLookupDto { Id = 5, Name = "Neil Diamond" }]);

        Assert.Single(display.Artists);
        Assert.Equal(5, display.Artists[0].ArtistId);
    }
}
