using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class LogPageTests : AuthPageTestContext
{
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();
    private readonly Mock<ILogPerformanceLocalStore> logStore = new();

    public LogPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });

        catalogLoader.Setup(loader => loader.LoadAsync(It.IsAny<Action<string>?>()))
            .ReturnsAsync(new LogCatalogSnapshot(
                Songs: [new LogSongPickItem(42, "Jeopardy", "The Greg Kihn Band", true, false)],
                RepertoireSongIds: [42],
                WorkingUpSongIds: [],
                FromCache: false,
                HasCatalog: true,
                CachedAtUtc: null));

        catalogLoader.Setup(loader => loader.LoadVenuesAsync())
            .ReturnsAsync(new VenueLoadResult(
                [new VenueDto { Id = 3, VenueName = "Main Stage" }],
                FromCache: false));

        logStore.Setup(store => store.GetRecentLogsAsync())
            .ReturnsAsync(Array.Empty<RecentLoggedPerformance>());

        logStore.Setup(store => store.GetFormDefaultsAsync())
            .ReturnsAsync((LogFormDefaults?)null);

        Api.Setup(client => client.GetMyListsAsync())
            .ReturnsAsync(SingerListsResult.Ok([]));

        Api.Setup(client => client.GetMySongSummaryAsync(42))
            .ReturnsAsync(SongSummaryResult.Ok(new SongPerformanceSummaryDto { SongId = 42 }));

        Api.Setup(client => client.GetSingersAsync())
            .ReturnsAsync([]);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        AddSyncfusionServices(services);
        services.AddSingleton(catalogLoader.Object);
        services.AddSingleton(logStore.Object);
    }

    [Fact]
    public void Shows_log_form_fields_when_song_is_selected()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/log?songId=42");

        var cut = Render<Log>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Venue", cut.Markup);
            Assert.Contains("Date", cut.Markup);
            Assert.Contains("Key", cut.Markup);
            Assert.Contains("Save performance", cut.Markup);
        });
    }
}
