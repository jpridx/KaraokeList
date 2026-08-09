using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Moq;

namespace KaraokeList.Web.Tests.Services;

public sealed class CatalogSyncServiceTests
{
    [Fact]
    public async Task SyncFromServerAsync_always_refreshes_tickler_settings()
    {
        var logCatalogLoader = new Mock<ILogCatalogLoader>();
        logCatalogLoader
            .Setup(l => l.LoadAsync(null))
            .ReturnsAsync(new LogCatalogSnapshot(
                Array.Empty<LogSongPickItem>(),
                new HashSet<int>(),
                new HashSet<int>(),
                FromCache: false,
                HasCatalog: true,
                CachedAtUtc: DateTime.UtcNow));

        var mySongsLoader = new Mock<IMySongsLoader>();
        mySongsLoader
            .Setup(l => l.LoadAsync(
                SingerListKind.MyRepertoire,
                "lastPerformed",
                "desc",
                null,
                null,
                null))
            .ReturnsAsync(new MySongsLoadResult(
                Array.Empty<SingerListDto>(),
                Array.Empty<RepertoireSongDto>(),
                Array.Empty<GenreDto>(),
                Array.Empty<string>(),
                Array.Empty<GenreGroupDto>(),
                FromCache: false,
                HasCache: true,
                CachedAtUtc: DateTime.UtcNow,
                ErrorMessage: null,
                NeedsSingerLink: false));

        var performancesLoader = new Mock<IMyPerformancesLoader>();
        performancesLoader
            .Setup(l => l.LoadAsync())
            .ReturnsAsync(new MyPerformancesLoadResult(
                Array.Empty<MyPerformanceEntryDto>(),
                FromCache: false,
                HasCache: true,
                CachedAtUtc: DateTime.UtcNow,
                ErrorMessage: null,
                NeedsSingerLink: false));

        var performanceCacheCoordinator = new Mock<IPerformanceCacheCoordinator>();
        performanceCacheCoordinator
            .Setup(c => c.RebuildRecentLogsFromPerformancesAsync())
            .Returns(Task.CompletedTask);

        var api = new Mock<IKaraokeApiClient>();
        api.Setup(a => a.GetTicklerSettingsAsync())
            .ReturnsAsync(TicklerSettingsResult.Ok(new TicklerSettingsDto { StaleAfterDays = 30, SongLimit = 3 }));

        var ticklerSettingsStore = new TicklerSettingsLocalStore(new InMemoryLocalStorage());
        var versionService = new Mock<ICatalogVersionService>();

        var sync = new CatalogSyncService(
            logCatalogLoader.Object,
            mySongsLoader.Object,
            performancesLoader.Object,
            performanceCacheCoordinator.Object,
            api.Object,
            ticklerSettingsStore,
            versionService.Object);

        var result = await sync.SyncFromServerAsync();

        Assert.True(result.Succeeded);
        api.Verify(a => a.GetTicklerSettingsAsync(), Times.Once);
        performanceCacheCoordinator.Verify(c => c.RebuildRecentLogsFromPerformancesAsync(), Times.Once);

        var cached = await ticklerSettingsStore.GetAsync();
        Assert.Equal(30, cached.StaleAfterDays);
        Assert.Equal(3, cached.SongLimit);
        versionService.Verify(v => v.Invalidate(), Times.Once);
    }
}
