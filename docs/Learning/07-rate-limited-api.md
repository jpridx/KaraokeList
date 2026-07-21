# 07 — Dealing with a Rate-Limited API

## Overview

KaraokeList deals with rate limits in **two directions**:

| Direction | Target | Mechanism |
|-----------|--------|-----------|
| **Outbound** | MusicBrainz (1 request/second guideline) | `FixedWindowRateLimiter` + Polly resilience on named `HttpClient` |
| **Inbound** | Login/register/password/OAuth endpoints | `AuthRateLimiter` per client key (IP) in memory |

Rate limiting is different from **retry**: limiters pace or reject; retries recover from transient failures. Both appear in `docs/resilience.md`.

## Major aspects

1. **Honor provider rules** — MusicBrainz expects polite usage (1 req/s) and a descriptive `User-Agent`.
2. **Fixed-window limiter** — permit 1 call per 1-second window; queue excess work instead of stampedes.
3. **Compose with retry + circuit breaker** — still need resilience after pacing.
4. **Named HttpClient** — `"MusicBrainz"` gets the special pipeline; other clients do not.
5. **Inbound auth throttles** — protect Identity endpoints from brute force / abuse.
6. **Do not confuse layers** — WASM read retries ≠ MusicBrainz outbound limit ≠ login rate limit.
7. **Test the pacing** — unit tests assert minimum spacing between outbound calls.

## Code samples

### Sample 1 — MusicBrainz 1 req/s fixed window

```12:42:KaraokeList.Api/Http/OutboundHttpResilience.cs
    public static FixedWindowRateLimiter CreateMusicBrainzRateLimiter() =>
        new(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 100
        });

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
```

### Sample 2 — Conceptual inbound auth throttle

```csharp
// KaraokeList.Api/Security/AuthRateLimiter.cs — AllowAttempt(action, clientKey, maxAttempts, window)
// Examples of policy intent (see source for exact numbers):
//   Login: limited attempts per rolling window
//   Register: stricter limit per hour
// Controllers call AllowAttempt before sensitive auth operations.
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Api/Program.cs` | ~55–73 | Registers `"MusicBrainz"` HttpClient + `AddMusicBrainzResilience` |
| `KaraokeList.Api/Security/AuthRateLimiter.cs` | ~16–63 | In-memory fixed windows for auth actions |
| `KaraokeList.Api.Tests/OutboundHttpResilienceTests.cs` | full | Asserts pacing between MusicBrainz calls |

## Exercises

1. **Multiple choice.** MusicBrainz’s polite usage target implemented here is:
   - A) 100 requests per second
   - B) 1 request per second
   - C) Unlimited with retries only
   - D) One request per day

2. **Fill in the blank.** Outbound pacing uses a ________WindowRateLimiter.

3. **Multiple choice.** Excess MusicBrainz calls (within QueueLimit) are:
   - A) Dropped silently forever
   - B) Queued (oldest first) until a permit is available
   - C) Converted to JWT claims
   - D) Sent to Application Insights as SQL

4. **Fill in the blank.** The named HttpClient for community music lookup is ________.

5. **Multiple choice.** `AuthRateLimiter` primarily protects:
   - A) Syncfusion rendering
   - B) Inbound auth endpoints from rapid repeated attempts
   - C) Service worker installs
   - D) Bicep deployments

6. **Fill in the blank.** MusicBrainz requests should identify the app via a ________ header.

7. **Multiple choice.** Rate limiting and retry differ because:
   - A) They are identical
   - B) Limiting paces/restricts call rate; retry re-attempts after transient failure
   - C) Retry only works on POST; limiting only on GET
   - D) Limiting requires Playwright

8. **Fill in the blank.** Circuit breaker sampling duration for outbound resilience is configured in ________.

9. **Multiple choice.** Why queue outbound MusicBrainz work instead of failing immediately?
   - A) To hide bugs
   - B) Batch/canonical jobs can issue many lookups without violating the 1 req/s rule
   - C) SQL Server requires queues
   - D) JWTs expire every second

10. **Fill in the blank.** Inbound limiter keys typically incorporate the client ________ (e.g. IP) plus action name.

## Answer key

1. B  
2. Fixed  
3. B  
4. `MusicBrainz`  
5. B  
6. `User-Agent`  
7. B  
8. `OutboundHttpResilience` (or `CircuitBreakerSamplingDuration`)  
9. B  
10. key / IP / address  
