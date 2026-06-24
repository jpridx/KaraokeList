namespace KaraokeList.Web.Services;

public sealed class SlowApiRequestHandler(ApiSlowRequestNotifier notifier) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var tracker = notifier.TrackRequest();
        var responseTask = base.SendAsync(request, cancellationToken);
        var delayTask = Task.Delay(ApiSlowRequestNotifier.SlowThreshold, cancellationToken);

        if (await Task.WhenAny(responseTask, delayTask) == delayTask)
        {
            tracker.MarkSlow();
        }

        return await responseTask;
    }
}
