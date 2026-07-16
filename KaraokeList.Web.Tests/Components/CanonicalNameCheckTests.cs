using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class CanonicalNameCheckTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();

    public CanonicalNameCheckTests()
    {
        Services.AddSingleton(api.Object);
    }

    [Fact]
    public void Shows_apply_metadata_button_when_names_already_match()
    {
        api.Setup(client => client.LookupCanonicalAsync(It.IsAny<CanonicalLookupRequest>()))
            .ReturnsAsync(new CanonicalLookupResponse
            {
                Match = new CanonicalMatchDto
                {
                    Found = true,
                    Title = "Zombie",
                    ArtistName = "The Cranberries",
                    ArtistCreditDisplay = "The Cranberries",
                    RecordingMbid = "mbid-1",
                    Year = 1994,
                    SuggestedGenreName = "Alternative Rock",
                    Score = 100
                }
            });

        var cut = Render<CanonicalNameCheck>(parameters => parameters
            .Add(p => p.Title, "Zombie")
            .Add(p => p.ArtistName, "The Cranberries"));

        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Names match MusicBrainz", cut.Markup);
            Assert.Contains("Apply MBID, genre", cut.Markup);
        }, timeout: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Apply_match_invokes_callback_with_year_and_genre()
    {
        CanonicalAppliedEventArgs? applied = null;
        api.Setup(client => client.LookupCanonicalAsync(It.IsAny<CanonicalLookupRequest>()))
            .ReturnsAsync(new CanonicalLookupResponse
            {
                Match = new CanonicalMatchDto
                {
                    Found = true,
                    Title = "Zombie",
                    ArtistName = "The Cranberries",
                    ArtistCreditDisplay = "The Cranberries",
                    RecordingMbid = "mbid-1",
                    Year = 1994,
                    SuggestedGenreName = "Alternative Rock",
                    Score = 100
                },
                Alternatives =
                [
                    new CanonicalMatchDto
                    {
                        Found = true,
                        Title = "Zombie",
                        ArtistName = "The Cranberries",
                        ArtistCreditDisplay = "The Cranberries",
                        RecordingMbid = "mbid-2",
                        Year = 1993,
                        SuggestedGenreName = "Rock",
                        Score = 90
                    }
                ]
            });

        var cut = Render<CanonicalNameCheck>(parameters => parameters
            .Add(p => p.Title, "Zombie")
            .Add(p => p.ArtistName, "The Cranberries")
            .Add(p => p.OnCanonicalApplied, EventCallback.Factory.Create<CanonicalAppliedEventArgs>(
                this, args => applied = args)));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll("button").Count > 1));

        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Apply MBID, genre & year", StringComparison.Ordinal) == true)
            .Click();

        Assert.NotNull(applied);
        Assert.Equal("mbid-1", applied.RecordingMbid);
        Assert.Equal(1994, applied.Year);
        Assert.Equal("Alternative Rock", applied.SuggestedGenreName);
    }

    [Fact]
    public void Alternative_match_button_applies_selected_match()
    {
        CanonicalAppliedEventArgs? applied = null;
        api.Setup(client => client.LookupCanonicalAsync(It.IsAny<CanonicalLookupRequest>()))
            .ReturnsAsync(new CanonicalLookupResponse
            {
                Match = new CanonicalMatchDto
                {
                    Found = true,
                    Title = "Zombie",
                    ArtistName = "The Cranberries",
                    ArtistCreditDisplay = "The Cranberries",
                    RecordingMbid = "mbid-1",
                    Year = 1994,
                    SuggestedGenreName = "Alternative Rock",
                    Score = 100
                },
                Alternatives =
                [
                    new CanonicalMatchDto
                    {
                        Found = true,
                        Title = "Zombie",
                        ArtistName = "The Cranberries",
                        ArtistCreditDisplay = "The Cranberries",
                        RecordingMbid = "mbid-2",
                        Year = 1993,
                        SuggestedGenreName = "Rock",
                        Score = 90
                    }
                ]
            });

        var cut = Render<CanonicalNameCheck>(parameters => parameters
            .Add(p => p.Title, "Zombie")
            .Add(p => p.ArtistName, "The Cranberries")
            .Add(p => p.OnCanonicalApplied, EventCallback.Factory.Create<CanonicalAppliedEventArgs>(
                this, args => applied = args)));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => cut.Markup.Contains("Use this match"));

        cut.FindAll("button")
            .First(button => button.TextContent?.Contains("Use this match", StringComparison.Ordinal) == true)
            .Click();

        Assert.NotNull(applied);
        Assert.Equal("mbid-2", applied.RecordingMbid);
        Assert.Equal(1993, applied.Year);
        Assert.Equal("Rock", applied.SuggestedGenreName);
    }
}
