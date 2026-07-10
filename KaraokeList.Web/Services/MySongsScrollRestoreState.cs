namespace KaraokeList.Web.Services;

public sealed record MySongsScrollRestore(int SongId, bool GroupByGenre);

public sealed class MySongsScrollRestoreState
{
    private int? pendingSongId;
    private bool pendingGroupByGenre;

    public void SetPending(int songId, bool groupByGenre)
    {
        pendingSongId = songId;
        pendingGroupByGenre = groupByGenre;
    }

    public MySongsScrollRestore? TryConsume(bool arrivedViaBackNavigation)
    {
        if (!arrivedViaBackNavigation || pendingSongId is not int songId)
        {
            pendingSongId = null;
            pendingGroupByGenre = false;
            return null;
        }

        pendingSongId = null;
        var groupByGenre = pendingGroupByGenre;
        pendingGroupByGenre = false;
        return new MySongsScrollRestore(songId, groupByGenre);
    }
}
