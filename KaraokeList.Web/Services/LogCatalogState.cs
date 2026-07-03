namespace KaraokeList.Web.Services;

public sealed class LogCatalogState
{
    public bool UsingOfflineCatalog { get; private set; }

    public bool HasCachedCatalog { get; private set; }

    public DateTime? CatalogCachedAt { get; private set; }

    public List<LogSongPickItem> SongPickerItems { get; private set; } = [];

    public HashSet<int> RepertoireSongIds { get; private set; } = [];

    public HashSet<int> WorkingUpSongIds { get; private set; } = [];

    public void Apply(LogCatalogSnapshot catalog)
    {
        UsingOfflineCatalog = catalog.FromCache;
        HasCachedCatalog = catalog.HasCatalog;
        CatalogCachedAt = catalog.CachedAtUtc;
        SongPickerItems = catalog.Songs.ToList();
        RepertoireSongIds = catalog.RepertoireSongIds;
        WorkingUpSongIds = catalog.WorkingUpSongIds;
    }

    /// <summary>
    /// Clears the offline flag without replacing catalog data.
    /// Used in the fast cache path where cached data is shown for performance,
    /// not because the API is unreachable.
    /// </summary>
    public void MarkOnline() => UsingOfflineCatalog = false;
}
