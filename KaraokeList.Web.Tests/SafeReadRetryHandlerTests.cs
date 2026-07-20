using System.Net;
using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests;

public sealed class SafeReadRetryHandlerTests
{
    [Fact]
    public async Task SendAsync_WhenGetReturns503ThenSuccess_RetriesOnce()
    {
        var attempts = 0;
        var inner = new StubHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        });
        var handler = new SafeReadRetryHandler { InnerHandler = inner };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var response = await client.GetAsync("api/songs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task SendAsync_WhenPostReturns503_DoesNotRetry()
    {
        var attempts = 0;
        var inner = new StubHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var handler = new SafeReadRetryHandler { InnerHandler = inner };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var response = await client.PostAsync("api/performances", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task SendAsync_WhenGetThrowsTransientException_RetriesOnce()
    {
        var attempts = 0;
        var inner = new StubHandler(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TaskCanceledException("timeout");
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var handler = new SafeReadRetryHandler { InnerHandler = inner };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var response = await client.GetAsync("api/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, attempts);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
