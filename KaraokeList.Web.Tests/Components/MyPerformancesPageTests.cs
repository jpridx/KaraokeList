using Blazored.LocalStorage;
using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class MyPerformancesPageTests : AuthPageTestContext
{
    private readonly InMemoryLocalStorage performancesLocalStorage = new();

    public MyPerformancesPageTests()
    {
        Api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });
        Api.Setup(client => client.GetVenuesAsync())
            .ReturnsAsync([]);
        Api.Setup(client => client.GetSingersAsync())
            .ReturnsAsync([]);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<IMyPerformancesLocalStore>(new MyPerformancesLocalStore(performancesLocalStorage));
        services.AddSingleton<IMyPerformancesLoader>(sp =>
            new MyPerformancesLoader(sp.GetRequiredService<IKaraokeApiClient>(),
                sp.GetRequiredService<IMyPerformancesLocalStore>()));
    }

    [Fact]
    public async Task Shows_a_just_logged_song_ahead_of_other_same_day_performances()
    {
        var store = new MyPerformancesLocalStore(performancesLocalStorage);
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 4,
                SongId = 7,
                Title = "Earlier Tonight",
                ArtistName = "Artist",
                VenueName = "Main Stage",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow.AddMinutes(-20)));

        var loader = new MyPerformancesLoader(Api.Object, store);
        await loader.AddPerformanceAsync(new MyPerformanceEntryDto
        {
            SongId = 42,
            Title = "Jeopardy",
            ArtistName = "The Greg Kihn Band",
            ArtistDisplay = "The Greg Kihn Band",
            VenueName = "Main Stage",
            PerformedOn = DateTime.Today
        });

        var refresh = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(refresh.Task);

        var cut = Render<MyPerformances>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Jeopardy", cut.Markup);
            Assert.Contains("Earlier Tonight", cut.Markup);
            Assert.True(cut.Markup.IndexOf("Jeopardy", StringComparison.Ordinal) <
                        cut.Markup.IndexOf("Earlier Tonight", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void Does_not_show_literal_loadError_when_performances_load()
    {
        Api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .ReturnsAsync(MyPerformancesResult.Ok(
            [
                new MyPerformanceEntryDto
                {
                    Id = 1,
                    SongId = 10,
                    Title = "Ticks",
                    ArtistName = "Brad Paisley",
                    PerformedOn = DateTime.Today
                }
            ]));

        var cut = Render<MyPerformances>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Ticks", cut.Markup);
            Assert.DoesNotContain(">loadError<", cut.Markup);
        });
    }

    [Fact]
    public async Task Renders_cached_performances_before_api_returns()
    {
        await performancesLocalStorage.SetItemAsync("karaoke.myPerformances.cached", new CachedMyPerformances(
            [new MyPerformanceEntryDto
            {
                Id = 9,
                SongId = 10,
                Title = "Cached Performance",
                ArtistName = "Cached Artist",
                PerformedOn = DateTime.Today
            }],
            DateTime.UtcNow));

        var tcs = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(tcs.Task);

        var cut = Render<MyPerformances>();

        cut.WaitForAssertion(() => Assert.Contains("Cached Performance", cut.Markup));
    }

    [Fact]
    public async Task Shows_offline_notice_when_api_fails_and_cache_exists()
    {
        await performancesLocalStorage.SetItemAsync("karaoke.myPerformances.cached", new CachedMyPerformances(
            [new MyPerformanceEntryDto
            {
                Id = 9,
                SongId = 10,
                Title = "Cached Performance",
                PerformedOn = DateTime.Today
            }],
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc)));

        Api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .ReturnsAsync(MyPerformancesResult.Fail("Cannot reach the API."));

        var cut = Render<MyPerformances>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Using cached performances", cut.Markup);
            Assert.Contains("Cached Performance", cut.Markup);
        });
    }

    [Fact]
    public void Shows_slow_api_notice_while_loading()
    {
        var tcs = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(tcs.Task);

        var notifier = Services.GetRequiredService<ApiSlowRequestNotifier>();
        using var tracker = notifier.TrackRequest();
        var cut = Render<MyPerformances>();

        tracker.MarkSlow();
        cut.Render();

        Assert.Contains(ApiTransientFailure.ColdStartInProgressMessage, cut.Markup);
    }
}
