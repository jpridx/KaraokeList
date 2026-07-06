namespace KaraokeList.Web.Services;

public sealed class ApiSlowRequestNotifier
{
    public static readonly TimeSpan SlowThreshold = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How long a foreground page load waits for the API before falling back to cached data
    /// and letting the request complete in the background.
    /// </summary>
    public static readonly TimeSpan PageLoadTimeout = TimeSpan.FromSeconds(10);

    private int slowRequestCount;

    public bool IsSlowLoading => Volatile.Read(ref slowRequestCount) > 0;

    public event Action? Changed;

    public IRequestTracker TrackRequest() => new RequestTracker(this);

    public interface IRequestTracker : IDisposable
    {
        void MarkSlow();
    }

    private sealed class RequestTracker(ApiSlowRequestNotifier notifier) : IRequestTracker
    {
        private int markedSlow;
        private int disposed;

        public void MarkSlow()
        {
            if (Interlocked.CompareExchange(ref markedSlow, 1, 0) != 0)
            {
                return;
            }

            Interlocked.Increment(ref notifier.slowRequestCount);
            notifier.Changed?.Invoke();
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
            {
                return;
            }

            if (markedSlow == 1)
            {
                Interlocked.Decrement(ref notifier.slowRequestCount);
            }

            notifier.Changed?.Invoke();
        }
    }
}
