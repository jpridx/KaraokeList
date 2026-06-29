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
            Artist = 10,
            Genre = 20,
            SecondaryArtist = 11
        };

        var display = SongDisplayMapper.ToDisplay(
            song,
            [new ArtistLookupDto { Id = 10, Name = "The Greg Kihn Band" }, new ArtistLookupDto { Id = 11, Name = "Guest" }],
            [new GenreDto { Id = 20, GenreName = "Rock" }]);

        Assert.Equal("The Greg Kihn Band", display.ArtistName);
        Assert.Equal("Rock", display.GenreName);
        Assert.Equal("Guest", display.SecondaryArtistName);
    }

    [Fact]
    public void ApplyArtistLookups_sets_foreign_keys_from_names()
    {
        var display = new SongDisplay
        {
            ArtistName = "Neil Diamond",
            SecondaryArtistName = ""
        };

        SongDisplayMapper.ApplyArtistLookups(
            display,
            [new ArtistLookupDto { Id = 5, Name = "Neil Diamond" }]);

        Assert.Equal(5, display.Artist);
        Assert.Null(display.SecondaryArtist);
    }
}
