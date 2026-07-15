using KaraokeList.Api.Services;
using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class SongAboutServiceTests
{
    [Fact]
    public void ToAboutDto_UsesArtistCreditDisplayWhenPresent()
    {
        var song = new SongDto
        {
            Id = 1,
            Title = "Islands in the Stream",
            Year = 1983,
            ArtistCreditDisplay = "Kenny Rogers feat. Dolly Parton",
            Artists =
            [
                new SongArtistDto { ArtistId = 10, DisplayOrder = 0, Name = "Kenny Rogers" },
                new SongArtistDto { ArtistId = 11, DisplayOrder = 1, Name = "Dolly Parton" }
            ]
        };

        var about = SongAboutService.ToAboutDto(song, "Country");

        Assert.Equal(1, about.SongId);
        Assert.Equal("Islands in the Stream", about.Title);
        Assert.Equal("Kenny Rogers feat. Dolly Parton", about.ArtistDisplay);
        Assert.Equal(1983, about.Year);
        Assert.Equal("Country", about.GenreName);
        Assert.Equal(["Kenny Rogers", "Dolly Parton"], about.ArtistNames);
        Assert.Null(about.Enrichment);
    }

    [Fact]
    public void ToAboutDto_JoinsArtistNamesWhenCreditDisplayMissing()
    {
        var song = new SongDto
        {
            Id = 2,
            Title = "Under Pressure",
            Artists =
            [
                new SongArtistDto { ArtistId = 20, DisplayOrder = 0, Name = "Queen" },
                new SongArtistDto { ArtistId = 21, DisplayOrder = 1, Name = "David Bowie" }
            ]
        };

        var about = SongAboutService.ToAboutDto(song, null);

        Assert.Equal("Queen, David Bowie", about.ArtistDisplay);
        Assert.Null(about.Year);
        Assert.Null(about.GenreName);
    }
}
