using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class MySongsScrollRestoreStateTests
{
    [Fact]
    public void TryConsume_returns_restore_when_arriving_via_back_navigation()
    {
        var state = new MySongsScrollRestoreState();
        state.SetPending(42);

        var result = state.TryConsume(arrivedViaBackNavigation: true);

        Assert.NotNull(result);
        Assert.Equal(42, result!.SongId);
    }

    [Fact]
    public void TryConsume_discards_pending_on_forward_navigation()
    {
        var state = new MySongsScrollRestoreState();
        state.SetPending(42);

        var result = state.TryConsume(arrivedViaBackNavigation: false);

        Assert.Null(result);
        Assert.Null(state.TryConsume(arrivedViaBackNavigation: true));
    }

    [Fact]
    public void TryConsume_returns_null_when_no_pending_song()
    {
        var state = new MySongsScrollRestoreState();

        Assert.Null(state.TryConsume(arrivedViaBackNavigation: true));
    }
}
