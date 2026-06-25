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
