using KaraokeList.Web.Services;

namespace KaraokeList.Web.Components;

public sealed record SongAddedEventArgs(
    int SongId,
    string Title,
    string ArtistName,
    LogCatalogSnapshot CatalogSnapshot);
