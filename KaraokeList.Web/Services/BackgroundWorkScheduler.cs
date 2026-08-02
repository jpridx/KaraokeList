namespace KaraokeList.Web.Services;

public interface IBackgroundWorkScheduler
{
    void Schedule(Func<Task> work);
}

public sealed class BackgroundWorkScheduler : IBackgroundWorkScheduler
{
    public void Schedule(Func<Task> work) => _ = RunSafely(work);

    private static async Task RunSafely(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch
        {
            // Fire-and-forget background work should not surface as unobserved exceptions.
        }
    }
}
