namespace KaraokeList.Shared;

public static class CatalogCachePolicy
{
    public static readonly TimeSpan RefreshThreshold = TimeSpan.FromHours(4);
}
