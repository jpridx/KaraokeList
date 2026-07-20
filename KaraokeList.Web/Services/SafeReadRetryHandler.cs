using KaraokeList.Shared;
using Polly;

namespace KaraokeList.Web.Services;

public sealed class SafeReadRetryHandler : DelegatingHandler
{
    private static readonly ResiliencePipeline<HttpResponseMessage> ReadRetryPipeline =
        ApiResiliencePolicies.CreateReadRetryPipeline();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
        {
            return base.SendAsync(request, cancellationToken);
        }

        return ReadRetryPipeline.ExecuteAsync(
            ct => SendOnceAsync(request, ct),
            cancellationToken).AsTask();
    }

    private ValueTask<HttpResponseMessage> SendOnceAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        new(base.SendAsync(request, cancellationToken));
}
