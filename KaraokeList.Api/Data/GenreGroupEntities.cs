namespace KaraokeList.Data;

public class GenreGroup
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<GenreGroupGenre> GenreMemberships { get; set; } = [];
}

public class GenreGroupGenre
{
    public int GenreGroupId { get; set; }
    public int GenreId { get; set; }
    public bool IsPrimary { get; set; }
    public GenreGroup? GenreGroup { get; set; }
    public Genre? Genre { get; set; }
}
