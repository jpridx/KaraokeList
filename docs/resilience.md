# HTTP resilience (Polly)

Issue [#264](https://github.com/jpridx/KaraokeList/issues/264) standardizes transient failure handling using **Polly** (via `Microsoft.Extensions.Http.Resilience` on the API and `Polly.Core` pipelines in Shared/Web).

Polly is a **resilience layer** — it retries failed or timed-out calls. It does **not** replace PWA offline caches, the slow-request notice, or optimistic UI. See [mobile-ux.md](mobile-ux.md) for those patterns.

## Where policies live

| Layer | Location | Behavior |
|-------|----------|----------|
| Transient classification | [`KaraokeList.Shared/ApiTransientFailure.cs`](../KaraokeList.Shared/ApiTransientFailure.cs) | Shared 408/502/503/504 + network/timeout exception checks |
| WASM read + auth retry math | [`KaraokeList.Shared/ApiResiliencePolicies.cs`](../KaraokeList.Shared/ApiResiliencePolicies.cs) | 2 attempts, 2s exponential backoff |
| WASM GET/HEAD retry | [`KaraokeList.Web/Services/SafeReadRetryHandler.cs`](../KaraokeList.Web/Services/SafeReadRetryHandler.cs) | Retries safe reads only; POST/PUT/DELETE pass through |
| WASM auth POST retry | [`KaraokeList.Web/Services/KaraokeApiClient.cs`](../KaraokeList.Web/Services/KaraokeApiClient.cs) `PostAuthAsync` | Retries login/register/link-singer; **no retry** on OAuth code exchange |
| API outbound HTTP | [`KaraokeList.Api/Http/OutboundHttpResilience.cs`](../KaraokeList.Api/Http/OutboundHttpResilience.cs) | MusicBrainz: 1 req/s rate limit + retry + circuit breaker; Google Sheets: retry + circuit breaker |
| EF Core SQL | [`KaraokeList.Api/Program.cs`](../KaraokeList.Api/Program.cs) | `EnableRetryOnFailure` (unchanged) |

### WASM HttpClient handler order (outer → inner)

`AuthorizationMessageHandler` → `SlowApiRequestHandler` → `SafeReadRetryHandler` → transport

The slow-request notice still fires during DB wake-up; Polly only helps after a failure or timeout.

### Intentionally not retried

- `POST api/performances`, catalog mutations (`TryPost*` / `TryPut*` / `TryDelete*`)
- OAuth external code exchange (single-use codes)
- Offline performance queue sync (stops on transient failure; item stays queued)

## Tests

Run:

```bash
dotnet test KaraokeList.Web.Tests/KaraokeList.Web.Tests.csproj
dotnet test KaraokeList.Api.Tests/KaraokeList.Api.Tests.csproj
```

| Test file | What it covers |
|-----------|----------------|
| [`ApiTransientFailureTests.cs`](../KaraokeList.Web.Tests/ApiTransientFailureTests.cs) | Transient status codes and exception types |
| [`ApiResiliencePoliciesTests.cs`](../KaraokeList.Web.Tests/ApiResiliencePoliciesTests.cs) | Shared pipeline retry counts and transient handling |
| [`SafeReadRetryHandlerTests.cs`](../KaraokeList.Web.Tests/SafeReadRetryHandlerTests.cs) | GET/HEAD retry; POST does not retry |
| [`KaraokeApiClientTests.cs`](../KaraokeList.Web.Tests/KaraokeApiClientTests.cs) | Auth retry on exception and 503; OAuth exchange does not retry |
| [`OutboundHttpResilienceTests.cs`](../KaraokeList.Api.Tests/OutboundHttpResilienceTests.cs) | Google Sheets retry on 503; MusicBrainz 1 req/s spacing |

The MusicBrainz rate-limit test uses real time and asserts ≥ 900ms between sequential calls (1s fixed window). It may be sensitive on very slow CI runners; increase the threshold only if flakes are observed.

## Related docs

- [mobile-refactor-roadmap.md](mobile-refactor-roadmap.md) — tier 6.1 (`IsOfflineFailure` dedup)
- [wasm-api-local-dev.md](wasm-api-local-dev.md) — local API + WASM setup
