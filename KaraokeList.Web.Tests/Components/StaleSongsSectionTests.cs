using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class StaleSongsSectionTests : AuthPageTestContext
{
    [Fact]
    public void Renders_nothing_when_no_stale_songs()
    {
        Api.Setup(client => client.GetMyStaleSongsAsync(null, null))
            .ReturnsAsync(StaleSongsResult.Ok(new StaleSongsResponseDto
            {
                StaleAfterDays = 90,
                Songs = []
            }));

        var cut = RenderComponent<StaleSongsSection>();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("Haven't sung in a while", cut.Markup));
    }

    [Fact]
    public void Shows_stale_songs_with_log_links()
    {
        var lastOn = new DateTime(2024, 1, 15);
        Api.Setup(client => client.GetMyStaleSongsAsync(null, null))
            .ReturnsAsync(StaleSongsResult.Ok(new StaleSongsResponseDto
            {
                StaleAfterDays = 90,
                Songs =
                [
                    new StaleSongDto
                    {
                        SongId = 42,
                        Title = "Footloose",
                        ArtistName = "Kenny Loggins",
                        LastPerformedOn = lastOn,
                        PerformanceCount = 3,
                        DaysSinceLastPerformed = 999
                    }
                ]
            }));

        var cut = RenderComponent<StaleSongsSection>();

        var expectedDays = PerformanceRelativeDate.FormatDaysSince(
            PerformanceRelativeDate.DaysSince(lastOn, DateTime.Today) ?? 0);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Haven't sung in a while", cut.Markup);
            Assert.Contains("Footloose", cut.Markup);
            Assert.Contains("href=\"log?songId=42\"", cut.Markup);
            Assert.Contains(expectedDays, cut.Markup);
            Assert.DoesNotContain("999 days ago", cut.Markup);
        });
    }

    [Fact]
    public void Shows_never_performed_repertoire_song()
    {
        Api.Setup(client => client.GetMyStaleSongsAsync(null, null))
            .ReturnsAsync(StaleSongsResult.Ok(new StaleSongsResponseDto
            {
                StaleAfterDays = 90,
                Songs =
                [
                    new StaleSongDto
                    {
                        SongId = 7,
                        Title = "New Song",
                        ArtistName = "Test Artist",
                        LastPerformedOn = null,
                        PerformanceCount = 0,
                        DaysSinceLastPerformed = 0
                    }
                ]
            }));

        var cut = RenderComponent<StaleSongsSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Never performed", cut.Markup);
            Assert.Contains("href=\"log?songId=7\"", cut.Markup);
        });
    }
}
