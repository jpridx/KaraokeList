using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class ApiSlowRequestNotifierTests
{
    [Fact]
    public void IsSlowLoading_false_until_request_is_marked_slow()
    {
        var notifier = new ApiSlowRequestNotifier();
        using var tracker = notifier.TrackRequest();

        Assert.False(notifier.IsSlowLoading);
    }

    [Fact]
    public void MarkSlow_sets_flag_until_tracker_disposed()
    {
        var notifier = new ApiSlowRequestNotifier();
        var tracker = notifier.TrackRequest();

        tracker.MarkSlow();
        Assert.True(notifier.IsSlowLoading);

        tracker.Dispose();
        Assert.False(notifier.IsSlowLoading);
    }

    [Fact]
    public void Multiple_slow_requests_keep_banner_until_all_complete()
    {
        var notifier = new ApiSlowRequestNotifier();
        var first = notifier.TrackRequest();
        var second = notifier.TrackRequest();

        first.MarkSlow();
        second.MarkSlow();
        Assert.True(notifier.IsSlowLoading);

        first.Dispose();
        Assert.True(notifier.IsSlowLoading);

        second.Dispose();
        Assert.False(notifier.IsSlowLoading);
    }

    [Fact]
    public void Changed_fires_when_slow_state_toggles()
    {
        var notifier = new ApiSlowRequestNotifier();
        var changes = 0;
        notifier.Changed += () => changes++;

        var tracker = notifier.TrackRequest();
        Assert.Equal(0, changes);

        tracker.MarkSlow();
        Assert.Equal(1, changes);

        tracker.Dispose();
        Assert.Equal(2, changes);
    }
}
