using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class StaleSongsSectionTests : BunitTestContext
{
    private readonly Mock<IStaleSongsLoader> loader = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(loader.Object);
    }

    [Fact]
    public void Renders_nothing_when_no_stale_songs()
    {
        loader.Setup(l => l.LoadAsync())
            .ReturnsAsync(StaleSongsLoadResult.Live(new StaleSongsResponseDto
            {
                StaleAfterDays = 90,
                Songs = []
            }));

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("Haven't sung in a while", cut.Markup));
    }

    [Fact]
    public void Shows_stale_songs_with_log_links()
    {
        var lastOn = new DateTime(2024, 1, 15);
        loader.Setup(l => l.LoadAsync())
            .ReturnsAsync(StaleSongsLoadResult.Live(new StaleSongsResponseDto
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

        var cut = Render<StaleSongsSection>();

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
        loader.Setup(l => l.LoadAsync())
            .ReturnsAsync(StaleSongsLoadResult.Live(new StaleSongsResponseDto
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

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Never performed", cut.Markup);
            Assert.Contains("href=\"log?songId=7\"", cut.Markup);
        });
    }

    [Fact]
    public void Shows_offline_notice_when_using_cached_data()
    {
        var cachedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        loader.Setup(l => l.LoadAsync())
            .ReturnsAsync(StaleSongsLoadResult.Cached(new StaleSongsResponseDto
            {
                StaleAfterDays = 90,
                Songs =
                [
                    new StaleSongDto
                    {
                        SongId = 5,
                        Title = "Livin' on a Prayer",
                        ArtistName = "Bon Jovi",
                        LastPerformedOn = null,
                        PerformanceCount = 0,
                        DaysSinceLastPerformed = 0
                    }
                ]
            }, cachedAt));

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Haven't sung in a while", cut.Markup);
            Assert.Contains("Using cached suggestions", cut.Markup);
        });
    }

    [Fact]
    public void Renders_nothing_when_api_fails_and_no_cache()
    {
        loader.Setup(l => l.LoadAsync())
            .ReturnsAsync(StaleSongsLoadResult.Failed("API unreachable."));

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("Haven't sung in a while", cut.Markup));
    }
}

