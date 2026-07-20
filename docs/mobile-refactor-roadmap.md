# Mobile refactor roadmap

Planning doc for the **architecture review** that started from [#70](https://github.com/jpridx/KaraokeList/issues/70) (componentize controls) and [#75](https://github.com/jpridx/KaraokeList/issues/75) (AddSongPanel flow polish).

| Phase | Audience | Tiers |
|-------|----------|-------|
| Mobile singer UX | Phone-first flows | **1–4.5** (done) |
| Admin catalog | Desktop grids under **More → Catalog** | **5** (done) |
| Infrastructure | API client, offline helpers, scaling | **6** (optional — piecemeal) |

See [mobile-ux.md](mobile-ux.md) for routes, flows, and the shared-components table.

## Status overview

| Tier | Theme | Status |
|------|--------|--------|
| **1** | High-ROI shared components | **Done** — PR [#98](https://github.com/jpridx/KaraokeList/pull/98) |
| **2** | Browse toolbars + performance edit helpers | **Done** — PR [#99](https://github.com/jpridx/KaraokeList/pull/99) |
| **3** | Mobile polish (shells, empty states, guidance) | **Done** — PR [#103](https://github.com/jpridx/KaraokeList/pull/103) |
| **4** | Structural page splits | **Done** — PR [#106](https://github.com/jpridx/KaraokeList/pull/106) |
| **4.5** | `SingerGatedPage` wrapper | **Done** — PR [#108](https://github.com/jpridx/KaraokeList/pull/108) |
| **5** | Admin catalog | **Done** |
| **6** | Infrastructure (optional) | **Not scheduled** — address individually when pain appears |

---

## Tier 1 — High-ROI extractions (done)

| Item | Delivered |
|------|-----------|
| Page chrome | `MobilePageHeader`, `OfflineCacheNotice` |
| Recent logs | `RecentLogList` |
| Performance editing | `PerformanceEditForm` |
| Singer resolution | `SingerProfileGate`, `SingerProfileResolver` |
| Log catalog picker | `CatalogSongMapper`, `CatalogSongPicker`, `LogCatalogLoader` |

---

## Tier 2 — Browse + edit helpers (done)

| Item | Delivered |
|------|-----------|
| Search + chips + paging | `MobileSearchToolbar`, `ChipFilterBar`, `LoadMoreButton` |
| Performance lists | `EditablePerformanceList`, `PerformanceEditOperations` |
| List membership | `SingerListResolver`, `SingerListActions` |
| Shell states | `MobileLoadingState`, `StatusAlerts`, `MobileEmptyState` |

---

## Tier 3 — Mobile polish (done)

Account shells, `MobileBackLink`, `SortDirectionToggle`, empty states, add-artist guidance — see PR [#103](https://github.com/jpridx/KaraokeList/pull/103).

---

## Tier 4 — Structural page splits (done)

My Songs, Log picker, song detail, invite share, `SingerGatedPage` — see PRs [#106](https://github.com/jpridx/KaraokeList/pull/106) and [#108](https://github.com/jpridx/KaraokeList/pull/108).

---

## Tier 5 — Admin catalog (done)

**Goal:** Safer, DRY admin grids under **More → Catalog**.

| # | Item | Delivered |
|---|------|-----------|
| **5.1** | Admin grid error handling | `TryCreate*` / `TryUpdate*` / `TryDelete*` on `IKaraokeApiClient`; `CatalogCrudGrid` cancels failed saves and shows `alert-danger` |
| **5.2** | `CatalogCrudGrid<T>` | Shared grid for Genres, Singers, Venues, Artists, Songs, Performances |
| **5.3** | Admin performance edit → `PerformanceEditOperations` | `SaveAdminAsync` + `TryDeletePerformanceAsync` on admin `/performances` |
| **5.4** | `SongDisplayMapper` | FK/display mapping extracted from `Songs.razor` into `SongDisplay` + mapper |
| **5.5** | bUnit smoke tests | `AdminCatalogPageTests` — one render test per admin catalog page |
| **5.6** | Shared test double | `NotImplementedApiClient` in `KaraokeList.Web.Tests/TestDoubles` |

**Key files:** `Components/CatalogCrudGrid.razor`, `Services/CatalogMutateResult.cs`, `Services/SongDisplayMapper.cs`, `Models/SongDisplay.cs`.

---

## Tier 6 — Infrastructure (optional / piecemeal)

**Not a scheduled batch.** Pick items when the corresponding pain shows up — there is no expectation that all of Tier 6 ships together.

| # | Item | When to consider |
|---|------|------------------|
| **6.1** | Move `IsOfflineFailure` to `ApiTransientFailure` | **Done** — deduped in loaders via shared `ApiTransientFailure` (PR #264) |
| **6.2** | Segment `IKaraokeApiClient` (admin / singer / auth) | When interface churn breaks test stubs |
| **6.3** | Generic `ApiResult<T>` instead of many result types | Large refactor; low urgency |
| **6.4** | UrlAdaptor + server paging for catalog grids | When catalog size outgrows in-memory grids |
| **6.5** | Split `QuickLogPerformance` into form + save service | If log flow keeps growing |

Mark individual 6.x items done in this table when shipped; no tier-wide exit criteria.

---

## Open feature backlog (not tiered)

| Issue | Topic |
|-------|--------|
| [#92](https://github.com/jpridx/KaraokeList/issues/92) | More flexible search — see [flexible-search-options.md](flexible-search-options.md) |
| [#57](https://github.com/jpridx/KaraokeList/issues/57) | Excel catalog upload |

---

## Working conventions

- **Branches:** `cursor/<short-description>-2e87` off `master`; PR into `master`.
- **Issues:** Put `Closes #nn` in the **PR body** for auto-close on squash merge.
- **Verification:** `dotnet build`; `dotnet test KaraokeList.Web.Tests/KaraokeList.Web.Tests.csproj`.
- **Docs:** Update this file when a tier item ships; add new components to [mobile-ux.md](mobile-ux.md).

## Related docs

| Doc | Role |
|-----|------|
| [mobile-ux.md](mobile-ux.md) | Routes, flows, shared components |
| [resilience.md](resilience.md) | Polly retry/rate-limit policies and unit tests (#264) |
| [Performances.md](Performances.md) | API + performance schema |
| [e2e-playwright.md](e2e-playwright.md) | Playwright setup |
| [flexible-search-options.md](flexible-search-options.md) | #92 design options (not implemented) |
