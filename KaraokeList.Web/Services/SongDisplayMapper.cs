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
        IReadOnlyList<GenreDto> genres)
    {
        var artists = song.Artists
            .OrderBy(a => a.DisplayOrder)
            .Select((artist, index) => new SongArtistDto
            {
                ArtistId = artist.ArtistId,
                DisplayOrder = index,
                Name = string.IsNullOrWhiteSpace(artist.Name)
                    ? ResolveArtistName(artist.ArtistId, artistLookups)
                    : artist.Name
            })
            .ToList();

        return new SongDisplay
        {
            Id = song.Id,
            Title = song.Title,
            Genre = song.Genre,
            GenreName = ResolveGenreName(song.Genre, genres),
            Year = song.Year,
            RecordingMbid = song.RecordingMbid,
            ArtistCreditDisplay = song.ArtistCreditDisplay,
            Artists = artists
        };
    }

    public static SongDto ToDto(SongDisplay display, IReadOnlyList<ArtistLookupDto> artistLookups)
    {
        ApplyArtistLookups(display, artistLookups);
        return new SongDto
        {
            Id = display.Id,
            Title = display.Title,
            Genre = display.Genre,
            Year = display.Year,
            RecordingMbid = display.RecordingMbid,
            ArtistCreditDisplay = display.ArtistCreditDisplay,
            Artists = display.Artists
                .OrderBy(a => a.DisplayOrder)
                .Select((artist, index) => new SongArtistDto
                {
                    ArtistId = artist.ArtistId,
                    DisplayOrder = index,
                    Name = artist.Name
                })
                .ToList()
        };
    }

    public static void ApplyArtistLookups(SongDisplay display, IReadOnlyList<ArtistLookupDto> artistLookups)
    {
        var resolved = new List<SongArtistDto>();
        for (var i = 0; i < display.Artists.Count; i++)
        {
            var entry = display.Artists[i];
            var name = entry.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var artistId = artistLookups.FirstOrDefault(a => a.Name == name)?.Id;
            if (artistId is not int id)
            {
                continue;
            }

            resolved.Add(new SongArtistDto
            {
                ArtistId = id,
                DisplayOrder = resolved.Count,
                Name = name
            });
        }

        display.Artists = resolved;
    }

    private static string ResolveArtistName(int artistId, IReadOnlyList<ArtistLookupDto> artistLookups) =>
        artistLookups.FirstOrDefault(a => a.Id == artistId)?.Name ?? string.Empty;

    private static string ResolveGenreName(int? genreId, IReadOnlyList<GenreDto> genres) =>
        genreId is int id
            ? genres.FirstOrDefault(g => g.Id == id)?.GenreName ?? string.Empty
            : string.Empty;
}
