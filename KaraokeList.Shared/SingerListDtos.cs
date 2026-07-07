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

public class ImportSingerListFromFileResponse : ImportSingerListSongsResponse
{
    public int TotalRows { get; set; }
    public int Matched { get; set; }
    public int NotFound { get; set; }
    public List<CatalogImportErrorDto> Errors { get; set; } = [];
}

public class ImportSingerListFromGSheetRequest
{
    public string SheetUrl { get; set; } = string.Empty;
    public SingerListKind ListKind { get; set; } = SingerListKind.MyRepertoire;
}

public class SongListMembershipDto
{
    public List<SingerListKind> Lists { get; set; } = [];
}
