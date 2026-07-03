namespace KaraokeList.Web.Services;

using KaraokeList.Shared;

public sealed record CachedSongEntry(int Id, string Title, string ArtistName);

public sealed record CachedVenueEntry(int Id, string VenueName);

public sealed record CachedLogCatalog(
    IReadOnlyList<CachedSongEntry> Songs,
    IReadOnlyList<int> RepertoireSongIds,
    IReadOnlyList<CachedVenueEntry> Venues,
    DateTime CachedAtUtc,
    IReadOnlyList<int>? WorkingUpSongIds = null,
    string? CacheTag = null);

public sealed record LogSongPickItem(int Id, string Title, string ArtistName, bool InRepertoire, bool InWorkingUp = false)
{
    public string Display
    {
        get
        {
            var text = $"{Title} - {ArtistName}";
            if (InRepertoire)
            {
                text += " ★";
            }

            if (InWorkingUp)
            {
                text += " 🎯";
            }

            return text;
        }
    }

    public string SearchKey => FlexibleSearch.Normalize($"{Title} {ArtistName}");
}

public sealed record LogCatalogSnapshot(
    IReadOnlyList<LogSongPickItem> Songs,
    HashSet<int> RepertoireSongIds,
    HashSet<int> WorkingUpSongIds,
    bool FromCache,
    bool HasCatalog,
    DateTime? CachedAtUtc);

public sealed record VenueLoadResult(IReadOnlyList<VenueDto> Venues, bool FromCache);
