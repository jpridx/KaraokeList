using KaraokeList.Data;
using KaraokeList.Shared;

namespace KaraokeList.Api.Mapping;

public static class EntityMappers
{
    public static VenueDto ToDto(this Venue entity) => new() { Id = entity.Id, VenueName = entity.VenueName };

    public static Venue ToEntity(this VenueDto dto) => new() { Id = dto.Id, VenueName = dto.VenueName };

    public static GenreDto ToDto(this Genre entity) => new() { Id = entity.Id, GenreName = entity.GenreName };

    public static Genre ToEntity(this GenreDto dto) => new() { Id = dto.Id, GenreName = dto.GenreName };

    public static ArtistDto ToDto(this Artist entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        SortableName = entity.SortableName,
        MainGenre = entity.MainGenre
    };

    public static Artist ToEntity(this ArtistDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        SortableName = dto.SortableName,
        MainGenre = dto.MainGenre
    };

    public static SingerDto ToDto(this Singer entity) => new() { Id = entity.Id, Name = entity.Name };

    public static Singer ToEntity(this SingerDto dto) => new() { Id = dto.Id, Name = dto.Name };

    public static SongDto ToDto(this Song entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Artist = entity.Artist,
        Genre = entity.Genre,
        Year = entity.Year,
        SecondaryArtist = entity.SecondaryArtist
    };

    public static Song ToEntity(this SongDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Artist = dto.Artist,
        Genre = dto.Genre,
        Year = dto.Year,
        SecondaryArtist = dto.SecondaryArtist
    };

    public static ArtistLookupDto ToDto(this ArtistLookup entity) => new() { Id = entity.Id, Name = entity.Name };

    public static SingerSongDto ToDto(this SingerSong entity) => new()
    {
        Id = entity.Id,
        Singer = entity.Singer,
        Song = entity.Song,
        Venue = entity.Venue,
        FirstSung = entity.FirstSung,
        LastSung = entity.LastSung,
        Count = entity.Count
    };

    public static SingerSong ToEntity(this SingerSongDto dto) => new()
    {
        Id = dto.Id,
        Singer = dto.Singer,
        Song = dto.Song,
        Venue = dto.Venue,
        FirstSung = dto.FirstSung,
        LastSung = dto.LastSung,
        Count = dto.Count
    };
}
