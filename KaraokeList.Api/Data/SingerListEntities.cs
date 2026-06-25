using KaraokeList.Shared;

namespace KaraokeList.Data;

public class SingerList
{
    public int Id { get; set; }
    public int SingerId { get; set; }
    public SingerListKind Kind { get; set; }
    public DateTime CreatedUtc { get; set; }
    public bool IsSystem { get; set; } = true;
    public Singer? Singer { get; set; }
    public ICollection<SingerListSong> Songs { get; set; } = [];
}

public class SingerListSong
{
    public int ListId { get; set; }
    public int SongId { get; set; }
    public DateTime AddedUtc { get; set; }
    public string? Notes { get; set; }
    public SingerList? List { get; set; }
    public Song? Song { get; set; }
}
