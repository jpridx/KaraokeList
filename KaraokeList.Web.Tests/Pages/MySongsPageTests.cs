using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

public sealed class MySongsPageTests : AuthPageTestContext
{
    private readonly Mock<IMySongsLoader> mySongsLoader = new();
    private readonly Mock<IScrollRestoreJs> scrollRestoreJs = new();
    private readonly InMemoryLocalStorage localStorage = new();

    public MySongsPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });

        mySongsLoader.Setup(loader => loader.NeedsRefreshAsync()).ReturnsAsync(false);

        scrollRestoreJs.Setup(js => js.ConsumeBackNavigationAsync()).ReturnsAsync(false);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        AddSyncfusionServices(services);
        services.AddSingleton(mySongsLoader.Object);
        services.AddSingleton<IMySongsLocalStore>(new MySongsLocalStore(localStorage));
        services.AddSingleton<MySongsScrollRestoreState>();
        services.AddSingleton(scrollRestoreJs.Object);
    }

    [Fact]
    public void Sort_reload_does_not_show_offline_cache_banner()
    {
        var cachedAt = DateTime.UtcNow;
        var initial = CreateLoadResult(fromCache: true, cachedAt);
        var reloaded = CreateLoadResult(fromCache: true, cachedAt);

        mySongsLoader.Setup(loader => loader.TryGetCachedAsync(
                It.IsAny<SingerListKind>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(initial);

        mySongsLoader.Setup(loader => loader.LoadAsync(
                It.IsAny<SingerListKind>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<Action<string>?>()))
            .ReturnsAsync(reloaded);

        var cut = Render<MySongs>();

        cut.WaitForAssertion(() => Assert.Contains("Jeopardy", cut.Markup));
        Assert.DoesNotContain("Using cached", cut.Markup);

        var sortToggle = cut.Find("button[title='Toggle sort direction']");
        sortToggle.Click();

        cut.WaitForAssertion(() =>
        {
            mySongsLoader.Verify(loader => loader.LoadAsync(
                It.IsAny<SingerListKind>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<Action<string>?>()), Times.Once);
            Assert.DoesNotContain("Using cached", cut.Markup);
        });
    }

    [Fact]
    public async Task Filter_reapply_does_not_show_offline_cache_banner()
    {
        var cachedAt = DateTime.UtcNow;
        var allSongs = CreateLoadResult(fromCache: true, cachedAt);
        var rockOnly = CreateLoadResult(fromCache: true, cachedAt, genreId: 10);

        mySongsLoader.Setup(loader => loader.TryGetCachedAsync(
                It.IsAny<SingerListKind>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>()))
            .ReturnsAsync((SingerListKind _, string _, string _, int? genreId, string? _) =>
                genreId == 10 ? rockOnly : allSongs);

        await localStorage.SetItemAsync("karaoke.mySongs.showGenreFilters", true);

        var cut = Render<MySongs>();

        cut.WaitForAssertion(() => Assert.Contains("Jeopardy", cut.Markup));
        Assert.DoesNotContain("Using cached", cut.Markup);

        var rockChip = cut.FindAll("button.genre-chip")
            .First(button => button.TextContent.Contains("Rock"));
        rockChip.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Using cached", cut.Markup);
            Assert.Contains("Jeopardy", cut.Markup);
        });
    }

    private static MySongsLoadResult CreateLoadResult(
        bool fromCache,
        DateTime cachedAt,
        int? genreId = null)
    {
        var songs = new List<RepertoireSongDto>
        {
            new()
            {
                SongId = 1,
                Title = "Jeopardy",
                ArtistName = "The Greg Kihn Band",
                GenreId = 10,
                GenreName = "Rock"
            }
        };

        if (genreId is int id)
        {
            songs = songs.Where(s => s.GenreId == id).ToList();
        }

        return new MySongsLoadResult(
            [new SingerListDto { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" }],
            songs,
            [new GenreDto { Id = 10, GenreName = "Rock" }],
            ["Rock"],
            [],
            fromCache,
            HasCache: true,
            cachedAt,
            null,
            false);
    }
}
