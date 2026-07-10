namespace KaraokeList.Web.Services;

public sealed record MySongsScrollRestore(int SongId);

public sealed class MySongsScrollRestoreState
{
    private int? pendingSongId;

    public void SetPending(int songId) => pendingSongId = songId;

    public MySongsScrollRestore? TryConsume(bool arrivedViaBackNavigation)
    {
        if (!arrivedViaBackNavigation || pendingSongId is not int songId)
        {
            pendingSongId = null;
            return null;
        }

        pendingSongId = null;
        return new MySongsScrollRestore(songId);
    }
}
