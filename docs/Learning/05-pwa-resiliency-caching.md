# 05 — PWA / Resiliency / Caching

## Overview

KaraokeList needs to feel reliable at a noisy venue: spotty Wi‑Fi, Azure SQL cold starts, and long MusicBrainz calls. Resilience is intentionally layered — these layers are **not interchangeable**:

| Layer | What it caches / protects | Technology |
|-------|---------------------------|------------|
| **PWA / service worker** | Static WASM assets (DLL, JS, CSS) | `service-worker.published.js` |
| **App data cache** | Catalog, My Songs, pending performances | Blazored LocalStorage |
| **HTTP resilience** | Transient GET/auth failures | Polly pipelines |
| **EF / outbound** | SQL retries; MusicBrainz rate limit | EF `EnableRetryOnFailure`, Polly |

See `docs/resilience.md` for the canonical policy map.

## Major aspects

1. **Service worker ≠ app data** — SW serves shell/assets; song lists and offline queues use LocalStorage.
2. **Published SW only** — development SW is network-only; production caches hashed assets.
3. **Cross-origin API bypass** — SW must not intercept API hosts (slow verify calls).
4. **Offline write queue** — Log performance can save locally and sync later.
5. **Retry GET, not blind POST** — safe-read retries; mutations generally do not auto-retry.
6. **Update banner** — detect new SW, `SKIP_WAITING`, reload for fresh assets.
7. **Cache tags / schema versions** — invalidate stale LocalStorage when catalog changes.

## Code samples

### Sample 1 — Published service worker caches assets, skips API hosts

```41:60:KaraokeList.Web/wwwroot/service-worker.published.js
async function onFetch(event) {
    const url = new URL(event.request.url);

    // API calls go to a different host and can run 30–60+ seconds (MusicBrainz verify).
    // Do not route them through the offline cache logic.
    if (url.origin !== self.location.origin) {
        return fetch(event.request);
    }
    // navigate → index.html; else cache.match then network
    let cachedResponse = false;
    if (event.request.method === 'GET') {
        const shouldServeIndexHtml = event.request.mode === 'navigate';
        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }
    return cachedResponse || fetch(event.request);
}
```

### Sample 2 — Polly read retry for transient failures

```13:25:KaraokeList.Shared/ApiResiliencePolicies.cs
    public static ResiliencePipeline<HttpResponseMessage> CreateReadRetryPipeline() =>
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = MaxReadAttempts - 1,
                Delay = RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<Exception>(ApiTransientFailure.IsTransient)
                    .HandleResult(response => ApiTransientFailure.IsTransient(response.StatusCode))
            })
            .Build();
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Web/Services/LogCatalogLoader.cs` | ~70–88 | Save catalog cache online; fall back on transient failure |
| `KaraokeList.Web/Services/PendingPerformanceSyncService.cs` | ~16–47 | Drain offline performance queue when back online |
| `docs/resilience.md` | full | Policy matrix: what retries, what queues, what does not |

## Exercises

1. **Multiple choice.** The service worker’s primary job in this app is to:
   - A) Store JWT secrets on the server
   - B) Cache static WASM assets for offline shell loading
   - C) Replace Azure SQL
   - D) Rate-limit MusicBrainz

2. **Fill in the blank.** Offline song/catalog data for the Log page is stored via Blazored ________.

3. **Multiple choice.** Why does the published SW bypass non-same-origin requests?
   - A) CORS forbids caching
   - B) API calls can be long-running and must not go through asset-cache logic
   - C) Polly cannot run in the browser
   - D) JWT tokens expire if cached

4. **Fill in the blank.** Safe HTTP retries for reads are implemented by ________ (library) pipelines in `ApiResiliencePolicies`.

5. **Multiple choice.** `SafeReadRetryHandler` retries which methods?
   - A) All methods including DELETE
   - B) GET and HEAD only
   - C) POST only
   - D) OPTIONS only

6. **Fill in the blank.** When a performance is saved offline, it waits in a ________ until sync succeeds.

7. **Multiple choice.** Activating a waiting service worker in this app uses the message type:
   - A) `RELOAD_NOW`
   - B) `SKIP_WAITING`
   - C) `CLEAR_JWT`
   - D) `SYNC_SQL`

8. **Fill in the blank.** Development service worker behavior is essentially ________-only (no offline asset cache).

9. **Multiple choice.** Azure SQL cold start is handled mainly by:
   - A) Increasing JWT lifetime
   - B) Client retries, slow-request UX, and EF retry — not by the service worker
   - C) Disabling CORS
   - D) Removing LocalStorage

10. **Fill in the blank.** The production worker file is ________.

## Answer key

1. B  
2. LocalStorage  
3. B  
4. Polly  
5. B  
6. queue (pending queue)  
7. B  
8. network  
9. B  
10. `service-worker.published.js`  
