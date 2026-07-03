using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record CachedListSongsEntry(SingerListKind Kind, IReadOnlyList<RepertoireSongDto> Songs);

public sealed record CachedMySongsLists(
    IReadOnlyList<SingerListDto> Lists,
    IReadOnlyList<CachedListSongsEntry> ListsSongs,
    DateTime CachedAtUtc,
    string? CacheTag = null);

public sealed record MySongsLoadResult(
    IReadOnlyList<SingerListDto> Lists,
    IReadOnlyList<RepertoireSongDto> Songs,
    IReadOnlyList<GenreDto> FilterGenres,
    bool FromCache,
    bool HasCache,
    DateTime? CachedAtUtc,
    string? ErrorMessage,
    bool NeedsSingerLink);
