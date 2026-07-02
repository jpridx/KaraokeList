using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class MusicServiceLinksTests
{
    [Theory]
    [InlineData(MusicService.Spotify, "Queen", "Bohemian Rhapsody", "https://open.spotify.com/search/Queen%20Bohemian%20Rhapsody")]
    [InlineData(MusicService.AppleMusic, "Queen", "Bohemian Rhapsody", "https://music.apple.com/search?term=Queen%20Bohemian%20Rhapsody")]
    [InlineData(MusicService.YouTubeMusic, "Queen", "Bohemian Rhapsody", "https://music.youtube.com/search?q=Queen%20Bohemian%20Rhapsody")]
    [InlineData(MusicService.AmazonMusic, "Queen", "Bohemian Rhapsody", "https://music.amazon.com/search/Queen%20Bohemian%20Rhapsody")]
    [InlineData(MusicService.Tidal, "Queen", "Bohemian Rhapsody", "https://listen.tidal.com/search?q=Queen%20Bohemian%20Rhapsody")]
    [InlineData(MusicService.Deezer, "Queen", "Bohemian Rhapsody", "https://www.deezer.com/search/Queen%20Bohemian%20Rhapsody")]
    public void BuildSearchUrl_ReturnsExpectedServiceUrl(MusicService service, string artist, string title, string expected)
    {
        Assert.Equal(expected, MusicServiceLinks.BuildSearchUrl(service, artist, title));
    }

    [Fact]
    public void BuildSearchUrl_None_ReturnsNull()
    {
        Assert.Null(MusicServiceLinks.BuildSearchUrl(MusicService.None, "Queen", "Bohemian Rhapsody"));
    }

    [Fact]
    public void BuildSearchQuery_UsesArtistAndTitle()
    {
        Assert.Equal("Queen Bohemian Rhapsody", MusicServiceLinks.BuildSearchQuery("Queen", "Bohemian Rhapsody"));
    }
}
