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
    private static readonly DateTime FixedLoadTimeUtc = new(2026, 8, 2, 18, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IMyPerformancesLoader> performancesLoader = new();
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();
    private readonly TestPerformanceCacheCoordinator performanceCache = new();
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
        services.AddSingleton<IBackgroundWorkScheduler, SynchronousBackgroundWorkScheduler>();
        services.AddSingleton(performancesLoader.Object);
        services.AddSingleton(catalogLoader.Object);
        services.AddSingleton<IPerformanceCacheCoordinator>(performanceCache);
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
            LoggedAt: new DateTime(2026, 6, 15, 12, 0, 0)));

        var cut = Render<TonightDashboard>();

        Assert.Contains("Recently logged", cut.Markup);
        Assert.Contains(performedOn.ToString("d"), cut.Markup);
        Assert.DoesNotContain(performedOn.ToString("t"), cut.Markup);
        Assert.Contains("Main Stage", cut.Markup);
    }

    [Fact]
    public async Task Recently_logged_skips_my_history_when_local_logs_are_fresh()
    {
        var store = (LogPerformanceLocalStore)Services.GetRequiredService<ILogPerformanceLocalStore>();
        var loggedAt = DateTime.Now.AddMinutes(-30);
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            SongId: 1,
            Title: "Tonight Song",
            ArtistName: "Artist",
            VenueName: "Main Stage",
            PerformedOn: loggedAt.Date,
            KeyChangeSemitones: null,
            LoggedAt: loggedAt));

        var cut = Render<TonightDashboard>();

        Assert.Contains("Tonight Song", cut.Markup);
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
            FixedLoadTimeUtc,
            null,
            false));

        var cut = Render<TonightDashboard>();

        Assert.Contains("Fresh July Song", cut.Markup);
        Assert.DoesNotContain("Stale June Song", cut.Markup);
        Assert.Contains(new DateTime(2026, 7, 9).ToString("d"), cut.Markup);
        performancesLoader.Verify(loader => loader.LoadAsync(), Times.Once);
    }

    [Fact]
    public async Task Recently_logged_keeps_local_entry_added_during_background_refresh()
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

        var loggedDuringRefresh = DateTime.Now.AddMinutes(-5);
        performancesLoader.Setup(loader => loader.LoadAsync())
            .Returns(async () =>
            {
                await store.AddRecentLogAsync(new RecentLoggedPerformance(
                    SongId: 2,
                    Title: "Just Logged",
                    ArtistName: "Artist",
                    VenueName: "Main Stage",
                    PerformedOn: loggedDuringRefresh.Date,
                    KeyChangeSemitones: null,
                    LoggedAt: loggedDuringRefresh));
                return new MyPerformancesLoadResult(
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
                FixedLoadTimeUtc,
                null,
                false);
            });

        var cut = Render<TonightDashboard>();

        Assert.Contains("Just Logged", cut.Markup);
        Assert.DoesNotContain("Fresh July Song", cut.Markup);

        var saved = await store.GetRecentLogsAsync();
        Assert.Equal("Just Logged", saved[0].Title);
    }

    [Fact]
    public async Task Recently_logged_hydrates_from_cache_using_cache_timestamp()
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

        var cacheTimeUtc = DateTime.UtcNow.AddHours(-1);
        var expectedLoggedAt = cacheTimeUtc.ToLocalTime();
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

        Assert.Contains("Cached July Song", cut.Markup);
        performancesLoader.Verify(loader => loader.LoadAsync(), Times.Never);

        var saved = await store.GetRecentLogsAsync();
        Assert.Equal(expectedLoggedAt, saved[0].LoggedAt);
    }

    [Fact]
    public async Task Recently_logged_refreshes_when_cache_coordinator_signals_change()
    {
        var store = (LogPerformanceLocalStore)Services.GetRequiredService<ILogPerformanceLocalStore>();
        var performedOn = new DateTime(2026, 8, 2);
        var loggedAt = DateTime.Now.AddMinutes(-30);
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            SongId: 1,
            Title: "Tonight Song",
            ArtistName: "Artist",
            VenueName: "Old Venue",
            PerformedOn: performedOn,
            KeyChangeSemitones: null,
            LoggedAt: loggedAt));

        var cut = Render<TonightDashboard>();
        Assert.Contains("Old Venue", cut.Markup);

        await store.PatchRecentLogAsync(
            1,
            performedOn,
            new RecentLoggedPerformance(
                1, "Tonight Song", "Artist", "New Venue", performedOn, null, loggedAt));
        performanceCache.RaiseRecentLogsChanged();
        cut.Render();

        Assert.Contains("New Venue", cut.Markup);
        Assert.DoesNotContain("Old Venue", cut.Markup);
    }

    [Fact]
    public async Task Default_venue_hidden_when_stored_default_is_from_yesterday()
    {
        var store = (LogPerformanceLocalStore)Services.GetRequiredService<ILogPerformanceLocalStore>();
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await store.SaveFormDefaultsAsync(new LogFormDefaults(3, yesterday));
        await store.AddRecentLogAsync(new RecentLoggedPerformance(
            SongId: 1,
            Title: "Tonight Song",
            ArtistName: "Artist",
            VenueName: "Main Stage",
            PerformedOn: DateTime.Today,
            KeyChangeSemitones: null,
            LoggedAt: DateTime.Now));

        catalogLoader.Setup(loader => loader.LoadVenuesAsync())
            .ReturnsAsync(new VenueLoadResult(
                [new VenueDto { Id = 3, VenueName = "Main Stage" }],
                FromCache: false));

        var cut = Render<TonightDashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tonight Song", cut.Markup);
            Assert.DoesNotContain("Default venue:", cut.Markup);
        });
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

        public void RaiseRecentLogsChanged() => RecentLogsChanged?.Invoke();
    }
}
