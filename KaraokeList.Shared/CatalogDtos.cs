namespace KaraokeList.Shared;

public class VenueDto
{
    public int Id { get; set; }
    public string VenueName { get; set; } = string.Empty;
}

public class GenreDto
{
    public int Id { get; set; }
    public string GenreName { get; set; } = string.Empty;
}

public class ArtistDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SortableName { get; set; }
    public int? MainGenre { get; set; }
}

public class SingerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SongDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? Artist { get; set; }
    public int? Genre { get; set; }
    public int? Year { get; set; }
    public int? SecondaryArtist { get; set; }
}

public class ArtistLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SingerSongDto
{
    public int Id { get; set; }
    public int? Singer { get; set; }
    public int? Song { get; set; }
    public int? Venue { get; set; }
    public DateTime? FirstSung { get; set; }
    public DateTime? LastSung { get; set; }
    public int Count { get; set; }
}
