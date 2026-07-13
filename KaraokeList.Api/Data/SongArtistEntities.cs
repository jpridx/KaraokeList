namespace KaraokeList.Data;

public class SongArtist
{
    public int SongId { get; set; }
    public int ArtistId { get; set; }
    public int DisplayOrder { get; set; }
    public Song? Song { get; set; }
    public Artist? Artist { get; set; }
}
