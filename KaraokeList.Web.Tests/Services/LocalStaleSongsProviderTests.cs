using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class LocalStaleSongsProviderTests
{
    private static readonly DateTime Today = new(2026, 6, 15);

    [Fact]
    public async Task ComputeAsync_normalizes_zero_song_limit_in_storage()
    {
        var localStorage = new InMemoryLocalStorage();
        var settingsStore = new TicklerSettingsLocalStore(localStorage);
        await settingsStore.SaveAsync(new TicklerSettingsDto { StaleAfterDays = 90, SongLimit = 0 });

        var logStore = new LogPerformanceLocalStore(localStorage);
        await logStore.SaveCachedCatalogAsync(new CachedLogCatalog(
            [],
            [1],
            [],
            DateTime.UtcNow,
            [],
            RepertoireStats:
            [
                new CachedRepertoireStatsEntry(1, "Stale", "Artist", "Artist", Today.AddDays(-120), 1)
            ]));

        var provider = CreateProvider(localStorage);
        var result = await provider.ComputeAsync(Today);

        Assert.True(result.HasSourceData);
        Assert.NotNull(result.Response);
        Assert.NotEmpty(result.Response!.Songs);
    }

    [Fact]
    public async Task ComputeAsync_falls_back_to_my_songs_when_primary_yields_no_candidates()
    {
        var localStorage = new InMemoryLocalStorage();
        var logStore = new LogPerformanceLocalStore(localStorage);
        await logStore.SaveCachedCatalogAsync(new CachedLogCatalog(
            [],
            [1, 2],
            [],
            new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
            [],
            RepertoireStats:
            [
                new CachedRepertoireStatsEntry(1, "Fresh", "Artist", "Artist", Today, 1),
                new CachedRepertoireStatsEntry(2, "Also Fresh", "Artist", "Artist", Today.AddDays(-10), 1)
            ]));

        var mySongsStore = new MySongsLocalStore(localStorage);
        await mySongsStore.SaveCachedListsAsync(new CachedMySongsLists(
            [],
            [
                new CachedListSongsEntry(
                    SingerListKind.MyRepertoire,
                    [
                        new RepertoireSongDto
                        {
                            SongId = 3,
                            Title = "Stale From My Songs",
                            ArtistName = "Artist",
                            LastPerformedOn = Today.AddDays(-120),
                            PerformanceCount = 1
                        }
                    ])
            ],
            new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)));

        var provider = CreateProvider(localStorage);
        var result = await provider.ComputeAsync(Today);

        Assert.True(result.HasSourceData);
        Assert.False(result.FromLogCache);
        Assert.Contains(result.Response!.Songs, song => song.SongId == 3);
    }

    [Fact]
    public async Task ComputeAsync_returns_no_source_data_when_caches_empty()
    {
        var provider = CreateProvider(new InMemoryLocalStorage());
        var result = await provider.ComputeAsync(Today);

        Assert.False(result.HasSourceData);
        Assert.Null(result.Response);
    }

    private static LocalStaleSongsProvider CreateProvider(InMemoryLocalStorage localStorage) =>
        new(
            new LogPerformanceLocalStore(localStorage),
            new MySongsLocalStore(localStorage),
            new TicklerSettingsLocalStore(localStorage),
            new TicklerExclusionsLocalStore(localStorage));
}
