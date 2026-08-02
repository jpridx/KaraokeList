using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class TonightDashboardTests : AuthPageTestContext
{
    private readonly Mock<IMyPerformancesLoader> performancesLoader = new();
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();
    private readonly InMemoryLocalStorage localStorage = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        catalogLoader.Setup(loader => loader.LoadVenuesAsync())
            .ReturnsAsync(new VenueLoadResult([], false));
        performancesLoader.Setup(loader => loader.TryGetCachedAsync())
            .ReturnsAsync((MyPerformancesLoadResult?)null);
        performancesLoader.Setup(loader => loader.LoadAsync())
            .ReturnsAsync(new MyPerformancesLoadResult([], false, false, null, null, false));
        services.AddSingleton(performancesLoader.Object);
        services.AddSingleton(catalogLoader.Object);
        services.AddSingleton<ILogPerformanceLocalStore>(new LogPerformanceLocalStore(localStorage));
    }

    [Fact]
    public async Task Recently_logged_shows_performance_date_not_time()
    {
        var store = (LogPerformanceLocalStore)Services.GetRequiredService<ILogPerformanceLocalStore>();
        var performedOn = new DateTime(2026, 6, 15);
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            SongId: 1,
            Title: "Jeopardy",
            ArtistName: "The Greg Kihn Band",
            VenueName: "Main Stage",
            PerformedOn: performedOn,
            KeyChangeSemitones: null,
            LoggedAt: DateTime.Now));

        var cut = Render<TonightDashboard>();
        cut.WaitForAssertion(() => Assert.Contains("Recently logged", cut.Markup));

        Assert.Contains(performedOn.ToString("d"), cut.Markup);
        Assert.DoesNotContain(performedOn.ToString("t"), cut.Markup);
        Assert.Contains("Main Stage", cut.Markup);
    }

    [Fact]
    public async Task Recently_logged_skips_my_history_when_local_logs_are_fresh()
    {
        var store = (LogPerformanceLocalStore)Services.GetRequiredService<ILogPerformanceLocalStore>();
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            SongId: 1,
            Title: "Tonight Song",
            ArtistName: "Artist",
            VenueName: "Main Stage",
            PerformedOn: DateTime.Today,
            KeyChangeSemitones: null,
            LoggedAt: DateTime.Now));

        var cut = Render<TonightDashboard>();
        cut.WaitForAssertion(() => Assert.Contains("Tonight Song", cut.Markup));

        performancesLoader.Verify(loader => loader.LoadAsync(), Times.Never);
        performancesLoader.Verify(loader => loader.TryGetCachedAsync(), Times.Never);
    }

    [Fact]
    public async Task Recently_logged_refreshes_from_api_when_local_storage_is_stale()
    {
        var store = (LogPerformanceLocalStore)Services.GetRequiredService<ILogPerformanceLocalStore>();
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            SongId: 99,
            Title: "Stale June Song",
            ArtistName: "Artist",
            VenueName: "Old Venue",
            PerformedOn: new DateTime(2026, 6, 11),
            KeyChangeSemitones: null,
            LoggedAt: new DateTime(2026, 6, 11)));

        performancesLoader.Setup(loader => loader.LoadAsync())
            .ReturnsAsync(new MyPerformancesLoadResult(
            [
                new MyPerformanceEntryDto
                {
                    SongId = 1,
                    Title = "Fresh July Song",
                    ArtistName = "Artist",
                    VenueName = "Main Stage",
                    PerformedOn = new DateTime(2026, 7, 9)
                }
            ],
            FromCache: false,
            HasCache: true,
            DateTime.UtcNow,
            null,
            false));

        var cut = Render<TonightDashboard>();
        cut.WaitForAssertion(() => Assert.Contains("Fresh July Song", cut.Markup));

        Assert.DoesNotContain("Stale June Song", cut.Markup);
        Assert.Contains(new DateTime(2026, 7, 9).ToString("d"), cut.Markup);
        performancesLoader.Verify(loader => loader.LoadAsync(), Times.Once);
    }

    [Fact]
    public async Task Recently_logged_hydrates_from_cache_without_stamping_loggedAt_as_now()
    {
        var store = (LogPerformanceLocalStore)Services.GetRequiredService<ILogPerformanceLocalStore>();
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            SongId: 99,
            Title: "Stale June Song",
            ArtistName: "Artist",
            VenueName: "Old Venue",
            PerformedOn: new DateTime(2026, 6, 11),
            KeyChangeSemitones: null,
            LoggedAt: new DateTime(2026, 6, 11)));

        var cacheTimeUtc = DateTime.UtcNow.AddHours(-3);
        performancesLoader.Setup(loader => loader.TryGetCachedAsync())
            .ReturnsAsync(new MyPerformancesLoadResult(
            [
                new MyPerformanceEntryDto
                {
                    SongId = 1,
                    Title = "Cached July Song",
                    ArtistName = "Artist",
                    VenueName = "Main Stage",
                    PerformedOn = new DateTime(2026, 7, 9)
                }
            ],
            FromCache: true,
            HasCache: true,
            cacheTimeUtc,
            null,
            false));

        var cut = Render<TonightDashboard>();
        cut.WaitForAssertion(() => Assert.Contains("Cached July Song", cut.Markup));

        performancesLoader.Verify(loader => loader.LoadAsync(), Times.Never);
        var saved = await store.GetRecentLogsAsync();
        Assert.Equal(cacheTimeUtc.ToLocalTime(), saved[0].LoggedAt);
        Assert.True(DateTime.Now - saved[0].LoggedAt > TimeSpan.FromHours(2));
    }
}
