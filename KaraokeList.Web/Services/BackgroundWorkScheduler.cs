namespace KaraokeList.Web.Services;

public interface IBackgroundWorkScheduler
{
    void Schedule(Func<Task> work);
}

public sealed class BackgroundWorkScheduler : IBackgroundWorkScheduler
{
    public void Schedule(Func<Task> work) => _ = work();
}
