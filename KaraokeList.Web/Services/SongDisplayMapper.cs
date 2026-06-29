using KaraokeList.Shared;
using KaraokeList.Web.Models;

namespace KaraokeList.Web.Services;

public static class SongDisplayMapper
{
    public static List<SongDisplay> ToDisplayList(
        IEnumerable<SongDto> songs,
        IReadOnlyList<ArtistLookupDto> artistLookups,
        IReadOnlyList<GenreDto> genres) =>
        songs.Select(song => ToDisplay(song, artistLookups, genres)).ToList();

    public static SongDisplay ToDisplay(
        SongDto song,
        IReadOnlyList<ArtistLookupDto> artistLookups,
        IReadOnlyList<GenreDto> genres) =>
        new()
        {
            Id = song.Id,
            Title = song.Title,
            Artist = song.Artist,
            ArtistName = ResolveArtistName(song.Artist, artistLookups),
            Genre = song.Genre,
            GenreName = ResolveGenreName(song.Genre, genres),
            Year = song.Year,
            SecondaryArtist = song.SecondaryArtist,
            SecondaryArtistName = ResolveArtistName(song.SecondaryArtist, artistLookups)
        };

    public static void ApplyArtistLookups(SongDisplay display, IReadOnlyList<ArtistLookupDto> artistLookups)
    {
        display.Artist = artistLookups.FirstOrDefault(a => a.Name == display.ArtistName)?.Id;
        display.SecondaryArtist = string.IsNullOrWhiteSpace(display.SecondaryArtistName)
            ? null
            : artistLookups.FirstOrDefault(a => a.Name == display.SecondaryArtistName)?.Id;
    }

    private static string ResolveArtistName(int? artistId, IReadOnlyList<ArtistLookupDto> artistLookups) =>
        artistId is int id
            ? artistLookups.FirstOrDefault(a => a.Id == id)?.Name ?? string.Empty
            : string.Empty;

    private static string ResolveGenreName(int? genreId, IReadOnlyList<GenreDto> genres) =>
        genreId is int id
            ? genres.FirstOrDefault(g => g.Id == id)?.GenreName ?? string.Empty
            : string.Empty;
}
