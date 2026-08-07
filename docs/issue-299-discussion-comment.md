# Copy everything below this line into GitHub issue #299

---

Discussion notes from architecture review (cache-first counterfactual, admin split, WinHost deployment).

---

## 1. What if WASM were cache-first from the start?

**Short answer:** Mobile singer flows already *behave* cache-first (stale-while-revalidate), but the architecture is still **network-first with feature-specific LocalStorage snapshots**. A true cache-first design would invert that: local persistence as the read source of truth, API as sync target.

### What the app does today

- **PWA service worker** — caches WASM shell/assets only
- **App data** — Blazored LocalStorage with ad-hoc DTOs (`CachedLogCatalog`, `CachedMySongsLists`, etc.)
- **Mobile SWR pattern** — Log, My Songs, and My Performances render cached data immediately, then refresh in background (`Log.razor.cs` fast path)
- **Invalidation** — coarse server `CacheTag` + 4-hour TTL (`CatalogCachePolicy`) + manual clear via More → Reload app
- **Offline writes** — only performance logging uses an outbox; everything else is online-only mutation
- **Admin catalog** — full API fetch on every visit; no local cache

### How cache-first-from-start would differ

| Area | Current (retrofitted) | Cache-first from day one |
|------|----------------------|--------------------------|
| Read path | HTTP → optionally write LocalStorage snapshot | Query local DB → render instantly; sync worker updates DB |
| Data model | Feature-specific JSON blobs | Normalized entities + sync metadata (`updatedAt`, `version`) |
| Invalidation | 4h TTL + global `CacheTag` | Per-entity versioning; delta sync |
| Offline scope | Read: Log/My Songs/My Performances; Write: performances only | Broader read cache; outbox for all mutations with conflict rules |
| Admin grids | Full catalog fetch every visit | Local DB query + background delta |

### What would stay the same

- Blazor WASM + REST API split
- JWT in LocalStorage for auth
- Client-side Syncfusion grids (UrlAdaptor optional either way)
- Server remains authoritative for shared catalog admin

### Recommendation

**No rewrite.** The retrofit matches the actual use case (spotty venue Wi-Fi on Log/My Songs). Incremental wins if needed: shared read store across mobile features, delta sync endpoint, IndexedDB only if catalog size forces it.

Key files: `Log.razor.cs`, `LogCatalogLoader.cs`, `LogPerformanceLocalStore.cs`, `CatalogSyncService.cs`, `CatalogCachePolicy.cs`, `docs/mobile-ux.md`, `docs/Learning/05-pwa-resiliency-caching.md`

---

## 2. Breaking catalog maintenance out to Blazor Server?

**Short answer:** **Moderately low advisability today** on Azure alone. Catalog maintenance is already API-backed; WASM admin pain (full-catalog loads, long MusicBrainz jobs, merge search) is fixable incrementally without reviving Blazor Server.

### Current state

- Blazor Server was **removed deliberately** (PR #40) when the stack settled on WASM + REST API
- Admin catalog was **rebuilt in WASM** under Tier 5 (~2k lines): `/admin/catalog`, `/admin/import-songs`, CRUD grids, `/admin/users`
- **Business logic is already on the API** — import sessions, MusicBrainz canonicization, verify batches, merge, CRUD
- **Do not move** with catalog maintenance: `/import-repertoire` (singer list import), Log-time add song/artist/venue, My Songs genre reclassification

### What Blazor Server would improve

- No full catalog in browser (merge search, in-memory grids)
- Long import/canonicalize jobs without browser timeout UX
- Smaller mobile WASM bundle (strip Syncfusion grids + admin pages)
- Proper desktop admin layout (admin currently uses mobile-page chrome)

### What it would cost

- Reverses a settled two-deployable architecture (WASM + API)
- Third runtime surface (or SignalR inside API)
- Auth duplication (JWT for mobile vs cookie/Identity for Server)
- ~2k lines of admin UI to re-port after Tier 5 just finished
- 1–2 admin users — Server circuit overhead rarely justified on Azure alone

### Lighter alternatives (no Server app)

1. **UrlAdaptor + server paging** (Tier 6.4) on existing API
2. **`GET api/songs/search?q=`** for merge picker (admin-only, paged)
3. **Background jobs** for canonicized import (API-side)
4. **Admin Razor Pages inside API** on same App Service — most Server benefits, one deploy

### Recommendation

**Don't split to Blazor Server on Azure right now.** Revisit when catalog outgrows in-memory grids or admin workload justifies a dedicated desktop tool. If splitting later, prefer **Razor Pages or Blazor Server hosted on the API App Service** at `/admin`, not reviving the old Server project.

---

## 3. WinHost deployment — how it changes the analysis

Deploying the management app on **WinHost raises advisability somewhat** — mainly because you avoid a separate Azure App Service and can co-locate admin with SQL and the API. It does **not** remove the engineering cost of porting admin UI.

### What WinHost improves vs Azure-only split

| Factor | Azure-only | WinHost admin |
|--------|------------|---------------|
| Extra hosting cost | New App Service | Another IIS site/folder on same plan (~$0 marginal) |
| Co-location with SQL | Often separate | Admin + API + SQL on one box — direct EF reads become realistic |
| Mobile PWA | Admin bloats WASM | Can strip admin from WASM; friends get smaller static app |
| Long imports | Browser polling/timeouts | Server-side on IIS |

### WinHost-specific risks

- **Cloudflare + SignalR** — admin hostname needs WebSockets enabled and cache bypass (same as `api.*`)
- **IIS app pool limits** on shared plans for huge MusicBrainz jobs
- **Dual-database trap** — admin on WinHost only works cleanly if mobile and admin share **one API and one database**

### Auth mismatch — why "move everything" to WinHost makes sense

If mobile WASM is on **Azure** and admin is on **WinHost**, you inherit cross-host complexity:

- JWT (mobile) vs cookie auth (admin desktop)
- CORS origins for a separate admin hostname
- Which API/DB is source of truth if Azure and WinHost each have SQL

**Moving everything to WinHost** (WASM + API + admin + SQL on one host) avoids that mismatch:

- One JWT issuer, one database, one CORS config
- Admin can use cookie auth on `admin.` (or `/admin` in API) while mobile keeps JWT — both against the **same** API and Identity store
- No cross-cloud auth or catalog sync problems

### Revised recommendation on WinHost

| Setup | Verdict |
|-------|---------|
| **All on WinHost** (WASM + API + admin + SQL) | **Moderate–good** — reasonable; split admin out; avoids auth/DB mismatch |
| **Admin co-hosted in API** on WinHost (`/admin`) | **Best ops tradeoff** — one Web Deploy, one app pool |
| **Azure mobile + WinHost admin, one DB** | **Moderate** — only if single source of truth is explicit |
| **Two live DBs (Azure + WinHost)** | **Avoid** |

---

## 4. WinHost layout sketch

### Option 1 — Everything on WinHost (simplest; avoids auth mismatch)

```
https://karaoke.yourdomain.net/     → WASM static (friends / PWA)
https://api.yourdomain.net/         → KaraokeList.Api (JWT REST)
https://admin.yourdomain.net/       → Blazor Server admin (catalog only)
         └── one WinHost SQL database (KaraokeList)
```

**Cloudflare:** WASM caches `/_framework/*`; `api.*` and `admin.*` bypass cache; **WebSockets enabled** on admin.

**Auth:** WASM → JWT (unchanged). Admin → cookie via Identity on WinHost.

**Move off WASM:** `/admin/catalog`, `/admin/import-songs`, CRUD grids, `/admin/users`.

**Keep on WASM:** `/log`, `/my-songs`, `/import-repertoire`, `/my-performances`, Tonight, etc.

### Option 2 — Admin co-hosted in API (fewest moving parts)

```
https://karaoke.yourdomain.net/     → WASM
https://api.yourdomain.net/admin/   → Blazor Server routes in API project
```

One Web Deploy target. Admin and API share Identity — cleanest auth story.

### Option 3 — Azure mobile + WinHost admin

Only viable if admin and mobile hit the **same API and same database**. Otherwise catalog edits diverge.

### Suggested rollout (if pursued)

1. Add `KaraokeList.Admin` or Server routes in API
2. Port `ImportSongs.razor` + `CatalogCrudGrid` pages first
3. Add server-side song search for merge
4. Deploy to WinHost; smoke-test import + verify
5. Remove admin routes from WASM
6. Update `docs/winhost-deployment.md`
