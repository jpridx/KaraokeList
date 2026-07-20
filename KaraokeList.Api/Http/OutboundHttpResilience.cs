using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace KaraokeList.Api.Http;

public static class OutboundHttpResilience
{
    public const int MaxRetryAttempts = 3;
    public static readonly TimeSpan CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);

    public static FixedWindowRateLimiter CreateMusicBrainzRateLimiter() =>
        new(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100
        });

    public static IHttpStandardResiliencePipelineBuilder AddGoogleSheetsResilience(this IHttpClientBuilder builder)
    {
        return builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = MaxRetryAttempts;
            options.CircuitBreaker.SamplingDuration = CircuitBreakerSamplingDuration;
        });
    }

    public static IHttpStandardResiliencePipelineBuilder AddMusicBrainzResilience(
        this IHttpClientBuilder builder,
        FixedWindowRateLimiter rateLimiter)
    {
        return builder.AddStandardResilienceHandler(options =>
        {
            options.RateLimiter = new HttpRateLimiterStrategyOptions
            {
                RateLimiter = args => rateLimiter.AcquireAsync(cancellationToken: args.Context.CancellationToken)
            };
            options.Retry.MaxRetryAttempts = MaxRetryAttempts;
            options.CircuitBreaker.SamplingDuration = CircuitBreakerSamplingDuration;
        });
    }
}
