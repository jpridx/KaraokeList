using System.Net;
using Polly;
using Polly.Retry;

namespace KaraokeList.Shared;

public static class ApiResiliencePolicies
{
    public const int MaxReadAttempts = 2;
    public const int MaxAuthAttempts = 2;
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public static ResiliencePipeline<HttpResponseMessage> CreateReadRetryPipeline() =>
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = MaxReadAttempts - 1,
                Delay = RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<Exception>(ApiTransientFailure.IsTransient)
                    .HandleResult(response => ApiTransientFailure.IsTransient(response.StatusCode))
            })
            .Build();

    public static ResiliencePipeline CreateAuthPostRetryPipeline() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxAuthAttempts - 1,
                Delay = RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder()
                    .Handle<Exception>(ApiTransientFailure.IsTransient)
                    .Handle<TransientHttpResponseException>()
            })
            .Build();
}

public sealed class TransientHttpResponseException(HttpStatusCode statusCode) : Exception
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
