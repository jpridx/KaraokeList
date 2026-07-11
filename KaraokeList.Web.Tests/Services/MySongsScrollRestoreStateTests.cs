using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class MySongsScrollRestoreStateTests
{
    [Fact]
    public void TryConsume_returns_restore_when_arriving_via_back_navigation()
    {
        var state = new MySongsScrollRestoreState();
        state.SetPending(42, groupByGenre: false);

        var result = state.TryConsume(arrivedViaBackNavigation: true);

        Assert.NotNull(result);
        Assert.Equal(42, result!.SongId);
        Assert.False(result.GroupByGenre);
    }

    [Fact]
    public void TryConsume_preserves_group_by_genre_flag()
    {
        var state = new MySongsScrollRestoreState();
        state.SetPending(42, groupByGenre: true);

        var result = state.TryConsume(arrivedViaBackNavigation: true);

        Assert.NotNull(result);
        Assert.True(result!.GroupByGenre);
        Assert.Null(result.GroupedVisibleLimit);
    }

    [Fact]
    public void TryConsume_preserves_grouped_visible_limit()
    {
        var state = new MySongsScrollRestoreState();
        state.SetPending(42, groupByGenre: true, groupedVisibleLimit: 80);

        var result = state.TryConsume(arrivedViaBackNavigation: true);

        Assert.NotNull(result);
        Assert.True(result!.GroupByGenre);
        Assert.Equal(80, result.GroupedVisibleLimit);
    }

    [Fact]
    public void TryConsume_discards_pending_on_forward_navigation()
    {
        var state = new MySongsScrollRestoreState();
        state.SetPending(42, groupByGenre: true);

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
