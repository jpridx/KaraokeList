namespace KaraokeList.Web.Services;

public sealed record TicklerExclusionsResult(bool Succeeded, IReadOnlyList<int>? SongIds, string? ErrorMessage)
{
    public static TicklerExclusionsResult Ok(IReadOnlyList<int> songIds) => new(true, songIds, null);
    public static TicklerExclusionsResult Fail(string message) => new(false, null, message);
}
