using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class SongAboutPanelTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();

    public SongAboutPanelTests()
    {
        Services.AddSingleton(api.Object);
    }

    [Fact]
    public void Renders_collapsed_about_this_song_summary()
    {
        var cut = Render<SongAboutPanel>(parameters => parameters.Add(p => p.SongId, 42));

        Assert.Contains("About this song", cut.Markup);
    }

    [Fact]
    public void Expanding_loads_catalog_and_enrichment_in_one_request()
    {
        api.Setup(client => client.GetSongAboutAsync(7, true))
            .ReturnsAsync(SongAboutResult.Ok(new SongAboutDto
            {
                SongId = 7,
                Title = "Zombie",
                ArtistDisplay = "The Cranberries",
                Year = 1994,
                GenreName = "Alternative Rock",
                Enrichment = new SongAboutEnrichmentDto
                {
                    NotableRelease = "No Need to Argue (1994)",
                    StyleTags = ["alternative rock"],
                    DurationMs = 306000,
                    ExternalUrl = "https://musicbrainz.org/recording/example"
                }
            }));

        var cut = Render<SongAboutPanel>(parameters => parameters.Add(p => p.SongId, 7));
        cut.Find("summary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Artists", cut.Markup);
            Assert.Contains("The Cranberries", cut.Markup);
            Assert.Contains("Released", cut.Markup);
            Assert.Contains("1994", cut.Markup);
            Assert.Contains("Genre", cut.Markup);
            Assert.Contains("Alternative Rock", cut.Markup);
            Assert.Contains("From", cut.Markup);
            Assert.Contains("No Need to Argue (1994)", cut.Markup);
            Assert.Contains("alternative rock", cut.Markup);
            Assert.Contains("5:06", cut.Markup);
            Assert.Contains("MusicBrainz", cut.Markup);
            api.Verify(client => client.GetSongAboutAsync(7, true), Times.Once);
        });
    }

    [Fact]
    public void Expanding_shows_empty_state_when_no_details_returned()
    {
        api.Setup(client => client.GetSongAboutAsync(7, true))
            .ReturnsAsync(SongAboutResult.Ok(new SongAboutDto
            {
                SongId = 7,
                Title = "Untitled"
            }));

        var cut = Render<SongAboutPanel>(parameters => parameters.Add(p => p.SongId, 7));
        cut.Find("summary").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("No extra details yet.", cut.Markup);
            api.Verify(client => client.GetSongAboutAsync(7, true), Times.Once);
        });
    }
}
