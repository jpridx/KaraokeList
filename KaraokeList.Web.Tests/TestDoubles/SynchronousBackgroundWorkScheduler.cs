using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.TestDoubles;

public sealed class SynchronousBackgroundWorkScheduler : IBackgroundWorkScheduler
{
    public void Schedule(Func<Task> work) => work().GetAwaiter().GetResult();
}
