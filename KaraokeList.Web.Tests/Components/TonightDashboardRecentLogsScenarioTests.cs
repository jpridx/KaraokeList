using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class TonightDashboardRecentLogsScenarioTests : AuthPageTestContext
{
    private readonly InMemoryLocalStorage localStorage = new();
    private MyPerformancesLocalStore perfStore = null!;
    private LogPerformanceLocalStore logStore = null!;
    private PerformanceCacheCoordinator coordinator = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        perfStore = new MyPerformancesLocalStore(localStorage);
        logStore = new LogPerformanceLocalStore(localStorage);
        var loader = new MyPerformancesLoader(new NotImplementedApiClient(), perfStore, logStore);
        coordinator = new PerformanceCacheCoordinator(loader, logStore, new Mock<IMySongsLoader>().Object);
        var catalogLoader = new Mock<ILogCatalogLoader>();
        catalogLoader.Setup(l => l.LoadVenuesAsync()).ReturnsAsync(new VenueLoadResult([], false));
        services.AddSingleton<IBackgroundWorkScheduler, SynchronousBackgroundWorkScheduler>();
        services.AddSingleton<IMyPerformancesLoader>(loader);
        services.AddSingleton(catalogLoader.Object);
        services.AddSingleton<IPerformanceCacheCoordinator>(coordinator);
        services.AddSingleton<ILogPerformanceLocalStore>(logStore);
    }

    private static MyPerformanceEntryDto P(int id, int songId, string title, DateTime on, string venue, int? key = null) =>
        new() { Id = id, SongId = songId, Title = title, ArtistName = "Artist" + songId, ArtistDisplay = "Artist" + songId,
                PerformedOn = on, VenueId = venue == "Bar" ? 1 : 2, VenueName = venue, KeyChangeSemitones = key };

    private async Task LogAsync(int id, int songId, string title)
    {
        var today = DateTime.Today;
        await logStore.AddRecentLogAsync(new(songId, title, "Artist" + songId, "Bar", today, 0, DateTime.Now));
        await coordinator.PatchAfterCreateAsync(P(id, songId, title, today, "Bar"));
        await Task.Delay(20);
    }

    private List<string> RenderHome()
    {
        var cut = Render<TonightDashboard>();
        var items = cut.FindAll(".recent-log-title")
            .Select(e => e.TextContent)
            .ToList();
        cut.Dispose();
        return items;
    }

    [Fact]
    public async Task Tonights_logs_stay_ahead_of_older_nights_across_home_visits_and_edits()
    {
        // Issues #335 / #347: hydrated older rows used to outrank, then evict, tonight's logs.
        var lastWeek = DateTime.Today.AddDays(-7);
        await perfStore.SaveCachedAsync(new CachedMyPerformances(
        [
            P(11, 3, "Last Week A", lastWeek, "Club"),
            P(10, 4, "Last Week B", lastWeek, "Club"),
        ], DateTime.UtcNow.AddMinutes(-30)));

        await LogAsync(20, 2, "Song One");
        Assert.Equal(["Song One - Artist2", "Last Week A - Artist3", "Last Week B - Artist4"], RenderHome());
        Assert.Equal(["Song One - Artist2", "Last Week A - Artist3", "Last Week B - Artist4"], RenderHome());

        await LogAsync(21, 7, "All I Ever Wanted");
        var before = new PerformanceEditSnapshot(
            21, 7, "All I Ever Wanted", "Artist7", "Artist7", DateTime.Today, 1, "Bar", null);
        await coordinator.PatchAfterUpdateAsync(before, before with { KeyChangeSemitones = -1 });

        Assert.Equal(["All I Ever Wanted - Artist7 (Down 1)", "Song One - Artist2", "Last Week A - Artist3"], RenderHome());
        Assert.Equal(["All I Ever Wanted - Artist7 (Down 1)", "Song One - Artist2", "Last Week A - Artist3"], RenderHome());
    }
}
