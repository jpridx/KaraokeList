using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class LogPageApiBudgetTests : AuthPageTestContext
{
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();
    private readonly Mock<ILogPerformanceLocalStore> logStore = new();
    private readonly Mock<IMyListsLoader> myListsLoader = new();
    private readonly Mock<IMySongsLoader> mySongsLoader = new();

    public LogPageApiBudgetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });

        catalogLoader.Setup(loader => loader.TryGetCachedAsync())
            .ReturnsAsync(new LogCatalogSnapshot(
                Songs: [new LogSongPickItem(42, "Jeopardy", "The Greg Kihn Band", true, false)],
                RepertoireSongIds: [42],
                WorkingUpSongIds: [],
                FromCache: true,
                HasCatalog: true,
                CachedAtUtc: DateTime.UtcNow));

        catalogLoader.Setup(loader => loader.NeedsRefreshAsync()).ReturnsAsync(false);
        catalogLoader.Setup(loader => loader.LoadVenuesAsync())
            .ReturnsAsync(new VenueLoadResult(
                [new VenueDto { Id = 3, VenueName = "Main Stage" }],
                FromCache: true));

        logStore.Setup(store => store.GetRecentLogsAsync())
            .ReturnsAsync(Array.Empty<RecentLoggedPerformance>());

        logStore.Setup(store => store.GetFormDefaultsAsync())
            .ReturnsAsync(new LogFormDefaults(3));

        myListsLoader.Setup(loader => loader.NeedsRefreshAsync()).ReturnsAsync(false);
        myListsLoader.Setup(loader => loader.TryGetCachedAsync())
            .ReturnsAsync(new MyListsBundle(
                [],
                new Dictionary<SingerListKind, IReadOnlyList<RepertoireSongDto>>(),
                [],
                Succeeded: true,
                FromCache: true,
                CachedAtUtc: DateTime.UtcNow));

        Api.Setup(client => client.GetMySongSummaryAsync(42))
            .ReturnsAsync(SongSummaryResult.Ok(new SongPerformanceSummaryDto
            {
                SongId = 42,
                PerformanceCount = 2,
                LastPerformedOn = DateTime.Today.AddDays(-30),
                LastVenueName = "Main Stage"
            }));

        Api.Setup(client => client.GetSingersAsync())
            .ReturnsAsync([]);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        AddSyncfusionServices(services);
        services.AddSingleton(catalogLoader.Object);
        services.AddSingleton(logStore.Object);
        services.AddSingleton(myListsLoader.Object);
        services.AddSingleton(mySongsLoader.Object);
    }

    [Fact]
    public void Selecting_song_fetches_my_song_summary_once_for_hint_and_form()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/log?songId=42");

        var cut = Render<Log>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Save performance", cut.Markup);
            Assert.Contains("you've sung this 2 time", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        Api.Verify(client => client.GetMySongSummaryAsync(42), Times.Once);
        Api.Verify(client => client.GetVenuesAsync(), Times.Never);
        catalogLoader.Verify(loader => loader.LoadVenuesAsync(), Times.Once);
        Api.Verify(client => client.GetSingersAsync(), Times.Once);
    }
}
