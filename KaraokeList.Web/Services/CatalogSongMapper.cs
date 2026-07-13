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
                ResolveArtistDisplay(s, artistNames),
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

    public static LogSongPickItem? FindCreatedPickItem(
        IEnumerable<LogSongPickItem> items,
        string title,
        string artistName) =>
        LogArtistPicker.FindCreatedSong(items, title, artistName, s => s.Title, s => s.ArtistName);

    private static string ResolveArtistDisplay(SongDto song, IReadOnlyDictionary<int, string> artistNames)
    {
        if (!string.IsNullOrWhiteSpace(song.ArtistCreditDisplay))
        {
            return song.ArtistCreditDisplay.Trim();
        }

        var names = song.Artists
            .OrderBy(a => a.DisplayOrder)
            .Select(a => !string.IsNullOrWhiteSpace(a.Name)
                ? a.Name
                : artistNames.GetValueOrDefault(a.ArtistId, string.Empty))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        return SongArtistFormatting.FormatDisplay(null, names);
    }
}
