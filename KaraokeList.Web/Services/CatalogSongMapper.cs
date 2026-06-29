using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class CatalogSongMapper
{
    public static List<LogSongPickItem> ToPickItems(
        IEnumerable<SongDto> songs,
        IReadOnlyDictionary<int, string> artistNames,
        IReadOnlySet<int>? repertoireSongIds = null,
        IReadOnlySet<int>? workingUpSongIds = null) =>
        songs
            .Select(s => new LogSongPickItem(
                s.Id,
                s.Title,
                s.Artist is int artistId && artistNames.TryGetValue(artistId, out var name) ? name : string.Empty,
                repertoireSongIds?.Contains(s.Id) == true,
                workingUpSongIds?.Contains(s.Id) == true))
            .OrderByDescending(s => s.InRepertoire)
            .ThenByDescending(s => s.InWorkingUp)
            .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static async Task<List<LogSongPickItem>> LoadPickItemsAsync(
        IKaraokeApiClient api,
        IReadOnlySet<int>? repertoireSongIds = null,
        IReadOnlySet<int>? workingUpSongIds = null)
    {
        var songs = await api.GetSongsAsync();
        var artists = await api.GetArtistLookupsAsync();
        var artistNames = artists.ToDictionary(a => a.Id, a => a.Name);
        return ToPickItems(songs, artistNames, repertoireSongIds, workingUpSongIds);
    }
}
