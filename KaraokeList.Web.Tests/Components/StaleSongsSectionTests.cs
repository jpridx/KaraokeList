using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class StaleSongsSectionTests : BunitTestContext
{
    private readonly Mock<ILocalStaleSongsProvider> provider = new();
    private readonly TestPerformanceCacheCoordinator performanceCache = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton(provider.Object);
        services.AddSingleton<IPerformanceCacheCoordinator>(performanceCache);
    }

    [Fact]
    public void Renders_nothing_when_no_stale_songs()
    {
        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto { StaleAfterDays = 90, Songs = [] },
                true,
                DateTime.UtcNow,
                true));

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("Haven't sung in a while", cut.Markup));
    }

    [Fact]
    public void Shows_stale_songs_with_log_links()
    {
        var lastOn = new DateTime(2024, 1, 15);
        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto
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
                },
                true,
                DateTime.UtcNow,
                true));

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
        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto
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
                },
                true,
                DateTime.UtcNow,
                true));

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Never performed", cut.Markup);
            Assert.Contains("href=\"log?songId=7\"", cut.Markup);
        });
    }

    [Fact]
    public void Shows_songs_from_cached_repertoire()
    {
        var cachedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto
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
                },
                true,
                cachedAt,
                true));

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Haven't sung in a while", cut.Markup);
            Assert.Contains("Livin' on a Prayer", cut.Markup);
            Assert.DoesNotContain("Using cached repertoire data", cut.Markup);
        });
    }

    [Fact]
    public void Renders_nothing_when_no_source_data()
    {
        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(null, false, null, false));

        var cut = Render<StaleSongsSection>();

        cut.WaitForAssertion(() =>
            Assert.DoesNotContain("Haven't sung in a while", cut.Markup));
    }

    [Fact]
    public void Refresh_suggestions_recomputes_with_new_random()
    {
        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto
                {
                    StaleAfterDays = 90,
                    Songs =
                    [
                        new StaleSongDto { SongId = 1, Title = "A", ArtistName = "X" }
                    ]
                },
                true,
                DateTime.UtcNow,
                true));

        var cut = Render<StaleSongsSection>();
        cut.WaitForAssertion(() => Assert.Contains("Refresh suggestions", cut.Markup));

        provider.Setup(p => p.ComputeAsync(null, It.IsAny<Random>()))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto
                {
                    StaleAfterDays = 90,
                    Songs =
                    [
                        new StaleSongDto { SongId = 2, Title = "B", ArtistName = "Y" }
                    ]
                },
                true,
                DateTime.UtcNow,
                true));

        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("B", cut.Markup);
        });

        provider.Verify(
            p => p.ComputeAsync(null, It.IsAny<Random>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void Recomputes_when_repertoire_stats_change()
    {
        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto
                {
                    StaleAfterDays = 90,
                    Songs =
                    [
                        new StaleSongDto { SongId = 1, Title = "First", ArtistName = "Artist" }
                    ]
                },
                true,
                DateTime.UtcNow,
                true));

        var cut = Render<StaleSongsSection>();
        cut.WaitForAssertion(() => Assert.Contains("First", cut.Markup));

        provider.Setup(p => p.ComputeAsync(null, null))
            .ReturnsAsync(new LocalStaleSongsResult(
                new StaleSongsResponseDto
                {
                    StaleAfterDays = 90,
                    Songs =
                    [
                        new StaleSongDto { SongId = 2, Title = "Updated", ArtistName = "Artist" }
                    ]
                },
                true,
                DateTime.UtcNow,
                true));

        performanceCache.RaiseRepertoireStatsChanged();
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Updated", cut.Markup);
            Assert.DoesNotContain("First", cut.Markup);
        });

        provider.Verify(p => p.ComputeAsync(null, null), Times.Exactly(2));
    }

    private sealed class TestPerformanceCacheCoordinator : IPerformanceCacheCoordinator
    {
        public event Action? RecentLogsChanged;

        public event Action? RepertoireStatsChanged;

        public Task PatchAfterUpdateAsync(PerformanceEditSnapshot before, PerformanceEditSnapshot after) =>
            Task.CompletedTask;

        public Task PatchAfterDeleteAsync(PerformanceEditSnapshot deleted) =>
            Task.CompletedTask;

        public Task RebuildRecentLogsFromPerformancesAsync() =>
            Task.CompletedTask;

        public void RaiseRepertoireStatsChanged() => RepertoireStatsChanged?.Invoke();
    }
}
