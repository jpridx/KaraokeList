using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class SingerListsResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public List<SingerListDto> Lists { get; init; } = [];

    public static SingerListsResult Ok(List<SingerListDto> lists) =>
        new() { Succeeded = true, Lists = lists };

    public static SingerListsResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public sealed class SingerListImportResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public ImportSingerListSongsResponse? Response { get; init; }

    public static SingerListImportResult Ok(ImportSingerListSongsResponse response) =>
        new() { Succeeded = true, Response = response };

    public static SingerListImportResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
