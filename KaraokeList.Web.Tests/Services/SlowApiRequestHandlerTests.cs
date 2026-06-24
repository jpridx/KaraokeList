using System.Net;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class SlowApiRequestHandlerTests
{
    [Fact]
    public async Task Fast_response_does_not_mark_request_slow()
    {
        var notifier = new ApiSlowRequestNotifier();
        var handler = new SlowApiRequestHandler(notifier)
        {
            InnerHandler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var response = await client.GetAsync("/api/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(notifier.IsSlowLoading);
    }

    [Fact]
    public async Task Slow_response_marks_request_slow_until_complete()
    {
        var gate = new TaskCompletionSource();
        var notifier = new ApiSlowRequestNotifier();
        var handler = new SlowApiRequestHandler(notifier)
        {
            InnerHandler = new StubHandler(async _ =>
            {
                await gate.Task;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var requestTask = client.GetAsync("/api/test");
        await Task.Delay(ApiSlowRequestNotifier.SlowThreshold + TimeSpan.FromMilliseconds(100));
        Assert.True(notifier.IsSlowLoading);

        gate.SetResult();
        var response = await requestTask;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(notifier.IsSlowLoading);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responder(request);
    }
}
