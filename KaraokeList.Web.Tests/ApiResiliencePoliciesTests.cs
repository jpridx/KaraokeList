using System.Net;
using KaraokeList.Shared;
using Polly;

namespace KaraokeList.Web.Tests;

public sealed class ApiResiliencePoliciesTests
{
    [Fact]
    public void Constants_MatchAuthAndReadRetryExpectations()
    {
        Assert.Equal(2, ApiResiliencePolicies.MaxReadAttempts);
        Assert.Equal(2, ApiResiliencePolicies.MaxAuthAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), ApiResiliencePolicies.RetryDelay);
    }

    [Fact]
    public async Task ReadRetryPipeline_When503ThenOk_RetriesOnce()
    {
        var attempts = 0;
        var pipeline = ApiResiliencePolicies.CreateReadRetryPipeline();

        var response = await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            await Task.CompletedTask;
            return new HttpResponseMessage(
                attempts == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ReadRetryPipeline_When503Twice_Returns503()
    {
        var attempts = 0;
        var pipeline = ApiResiliencePolicies.CreateReadRetryPipeline();

        var response = await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(ApiResiliencePolicies.MaxReadAttempts, attempts);
    }

    [Fact]
    public async Task AuthPostRetryPipeline_WhenTransientHttpResponseException_RetriesOnce()
    {
        var attempts = 0;
        var pipeline = ApiResiliencePolicies.CreateAuthPostRetryPipeline();

        await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            await Task.CompletedTask;
            if (attempts == 1)
            {
                throw new TransientHttpResponseException(HttpStatusCode.BadGateway);
            }
        });

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task AuthPostRetryPipeline_WhenNonTransientException_DoesNotRetry()
    {
        var attempts = 0;
        var pipeline = ApiResiliencePolicies.CreateAuthPostRetryPipeline();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pipeline.ExecuteAsync(async _ =>
            {
                attempts++;
                await Task.CompletedTask;
                throw new InvalidOperationException("permanent");
            }));

        Assert.Equal(1, attempts);
    }
}
