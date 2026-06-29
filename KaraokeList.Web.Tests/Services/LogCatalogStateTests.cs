using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class LogCatalogStateTests
{
    [Fact]
    public void Apply_copies_catalog_snapshot_fields()
    {
        var cachedAt = DateTime.UtcNow.AddHours(-1);
        var snapshot = new LogCatalogSnapshot(
            Songs:
            [
                new LogSongPickItem(1, "Jeopardy", "The Greg Kihn Band", true, false)
            ],
            RepertoireSongIds: [1],
            WorkingUpSongIds: [2],
            FromCache: true,
            HasCatalog: true,
            CachedAtUtc: cachedAt);

        var state = new LogCatalogState();
        state.Apply(snapshot);

        Assert.True(state.UsingOfflineCatalog);
        Assert.True(state.HasCachedCatalog);
        Assert.Equal(cachedAt, state.CatalogCachedAt);
        Assert.Single(state.SongPickerItems);
        Assert.Contains(1, state.RepertoireSongIds);
        Assert.Contains(2, state.WorkingUpSongIds);
    }
}
