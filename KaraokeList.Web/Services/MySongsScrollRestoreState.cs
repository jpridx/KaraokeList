namespace KaraokeList.Web.Services;

public sealed record MySongsScrollRestore(int SongId, bool GroupByGenre, int? GroupedVisibleLimit);

public sealed class MySongsScrollRestoreState
{
    private int? pendingSongId;
    private bool pendingGroupByGenre;
    private int? pendingGroupedVisibleLimit;

    public void SetPending(int songId, bool groupByGenre, int? groupedVisibleLimit = null)
    {
        pendingSongId = songId;
        pendingGroupByGenre = groupByGenre;
        pendingGroupedVisibleLimit = groupedVisibleLimit;
    }

    public MySongsScrollRestore? TryConsume(bool arrivedViaBackNavigation)
    {
        if (!arrivedViaBackNavigation || pendingSongId is not int songId)
        {
            pendingSongId = null;
            pendingGroupByGenre = false;
            pendingGroupedVisibleLimit = null;
            return null;
        }

        pendingSongId = null;
        var groupByGenre = pendingGroupByGenre;
        var groupedVisibleLimit = pendingGroupedVisibleLimit;
        pendingGroupByGenre = false;
        pendingGroupedVisibleLimit = null;
        return new MySongsScrollRestore(songId, groupByGenre, groupedVisibleLimit);
    }
}
