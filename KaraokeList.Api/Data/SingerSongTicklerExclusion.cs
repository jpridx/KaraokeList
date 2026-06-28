namespace KaraokeList.Data;

public class SingerSongTicklerExclusion
{
    public int SingerId { get; set; }
    public int SongId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedUtc { get; set; }
    public Singer? Singer { get; set; }
    public Song? Song { get; set; }
}
