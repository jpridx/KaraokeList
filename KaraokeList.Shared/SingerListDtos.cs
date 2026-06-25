namespace KaraokeList.Shared;

public enum SingerListKind
{
    MyRepertoire,
    WantToSing,
    WorkingUp
}

public static class SingerListKindNames
{
    public static string DisplayName(SingerListKind kind) => kind switch
    {
        SingerListKind.MyRepertoire => "My repertoire",
        SingerListKind.WantToSing => "Want to sing",
        SingerListKind.WorkingUp => "Working up",
        _ => kind.ToString()
    };
}

public class SingerListDto
{
    public int Id { get; set; }
    public SingerListKind Kind { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public class AddSingerListSongRequest
{
    public int SongId { get; set; }
}

public class ImportSingerListSongsRequest
{
    public SingerListKind ListKind { get; set; }
    public List<int> SongIds { get; set; } = [];
}

public class ImportSingerListSongsResponse
{
    public int Added { get; set; }
    public int Skipped { get; set; }
    public int Rejected { get; set; }
}
