using System.Collections.Concurrent;
using System.Net;
using KaraokeList.Api.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KaraokeList.Api.Tests;

public sealed class OutboundHttpResilienceTests
{
    [Fact]
    public async Task GoogleSheetsClient_When503ThenSuccess_RetriesOnce()
    {
        var attempts = 0;
        using var client = CreateGoogleSheetsClient(() =>
        {
            attempts++;
            return new HttpResponseMessage(
                attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        });

        var response = await client.GetAsync("sheet");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task MusicBrainzClient_WhenTwoSequentialRequests_SecondWaitsForRateLimit()
    {
        var timestamps = new ConcurrentBag<DateTimeOffset>();
        using var client = CreateMusicBrainzClient(() =>
        {
            timestamps.Add(DateTimeOffset.UtcNow);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await client.GetAsync("recording/1");
        await client.GetAsync("recording/2");

        var ordered = timestamps.OrderBy(timestamp => timestamp).ToList();
        Assert.Equal(2, ordered.Count);
        Assert.True(
            ordered[1] - ordered[0] >= TimeSpan.FromMilliseconds(900),
            $"Expected at least 900ms between MusicBrainz calls, got {(ordered[1] - ordered[0]).TotalMilliseconds}ms.");
    }

    private static HttpClient CreateGoogleSheetsClient(Func<HttpResponseMessage> responder)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Warning));

        services.AddHttpClient("GoogleSheets", client =>
            {
                client.BaseAddress = new Uri("https://resilience.test/");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new CountingHandler(responder))
            .AddGoogleSheetsResilience();

        return services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("GoogleSheets");
    }

    private static HttpClient CreateMusicBrainzClient(Func<HttpResponseMessage> responder)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Warning));

        var rateLimiter = OutboundHttpResilience.CreateMusicBrainzRateLimiter();
        services.AddSingleton(rateLimiter);

        services.AddHttpClient("MusicBrainz", client =>
            {
                client.BaseAddress = new Uri("https://resilience.test/");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new CountingHandler(responder))
            .AddMusicBrainzResilience(rateLimiter);

        return services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("MusicBrainz");
    }

    private sealed class CountingHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder());
    }
}
