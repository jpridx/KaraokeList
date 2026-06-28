namespace KaraokeList.Web.Services;

public sealed class AppUpdateNotifier
{
    public bool UpdateAvailable { get; private set; }

    public event Action? Changed;

    public void MarkUpdateAvailable()
    {
        if (UpdateAvailable)
        {
            return;
        }

        UpdateAvailable = true;
        Changed?.Invoke();
    }
}
