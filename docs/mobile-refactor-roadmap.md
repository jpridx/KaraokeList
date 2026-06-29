# Mobile refactor roadmap

Planning doc for the **architecture review** that started from [#70](https://github.com/jpridx/KaraokeList/issues/70) (componentize controls) and [#75](https://github.com/jpridx/KaraokeList/issues/75) (AddSongPanel flow polish).

| Phase | Audience | Tiers |
|-------|----------|-------|
| Mobile singer UX | Phone-first flows | **1–4.5** (done) |
| Admin catalog | Desktop grids under **More → Catalog** | **5** (planned) |
| Infrastructure | API client, offline helpers, scaling | **6** (optional / when needed) |

See [mobile-ux.md](mobile-ux.md) for routes, flows, and the shared-components table.

## Status overview

| Tier | Theme | Status |
|------|--------|--------|
| **1** | High-ROI shared components | **Done** — PR [#98](https://github.com/jpridx/KaraokeList/pull/98) |
| **2** | Browse toolbars + performance edit helpers | **Done** — PR [#99](https://github.com/jpridx/KaraokeList/pull/99) (+ follow-up commits on same branch) |
| **3** | Mobile polish (shells, empty states, guidance) | **Done** — PR [#103](https://github.com/jpridx/KaraokeList/pull/103) |
| **4** | Structural page splits | **Done** — PR [#106](https://github.com/jpridx/KaraokeList/pull/106) |
| **4.5** | `SingerGatedPage` wrapper | **Done** — PR [#108](https://github.com/jpridx/KaraokeList/pull/108) |
| **5** | Admin catalog | **Planned** |
| **6** | Infrastructure (optional / when scaling) | **Planned** |

Post–Tier 4 bugfix PRs ([#107](https://github.com/jpridx/KaraokeList/pull/107) literal bindings, [#109](https://github.com/jpridx/KaraokeList/issues/109)/[#110](https://github.com/jpridx/KaraokeList/pull/110) Log fields, [#111](https://github.com/jpridx/KaraokeList/pull/111) PWA banner text) are not separate tiers.

---

## Tier 1 — High-ROI extractions (done)

**Goal:** Remove the most repeated mobile scaffolding without changing behavior.

| Item | Delivered |
|------|-----------|
| Page chrome | `MobilePageHeader`, `OfflineCacheNotice` |
| Recent logs | `RecentLogList` |
| Performance editing | `PerformanceEditForm` |
| Singer resolution | `SingerProfileGate`, `SingerProfileResolver`, `SingerLinkPanel` on admin Performances |
| Log catalog picker | `CatalogSongMapper`, `CatalogSongPicker`, `LogCatalogLoader` |

---

## Tier 2 — Browse + edit helpers (done)

**Goal:** Deduplicate mobile list/browse pages and performance list edit logic.

| Item | Delivered |
|------|-----------|
| Search + chips + paging | `MobileSearchToolbar`, `ChipFilterBar`, `LoadMoreButton` |
| Stale songs links | `SongLinkList` |
| Performance CRUD helpers | `PerformanceEditOperations` |
| Unified performance lists | `EditablePerformanceList` (replaces `PerformanceBrowseList` / `PerformanceHistoryList`) |
| List membership | `SingerListResolver`, `SingerListActions` |
| Song stats block | `SongPerformanceSummaryPanel`, catalog post-create helpers |
| Shell states | `MobileLoadingState`, `StatusAlerts`, `MobileEmptyState` |
| Gate extensions | `SingerProfileGateExtensions.RequireLinkIfNotLinked` |

---

## Tier 3 — Mobile polish (done)

**Goal:** Consistent account/settings shells, navigation, and form guidance.

| Item | Delivered |
|------|-----------|
| 3.1 | `MobileBackLink`; `MobilePageHeader` back link params |
| 3.2 | Account pages → `MobileLoadingState` + `StatusAlerts` |
| 3.3 | `QuickLogPerformance` → `StatusAlerts` |
| 3.4 | `ChipFilterBuilder.CreateAllPlusItems` |
| 3.5 | `SortDirectionToggle` |
| 3.6 | `NoPerformancesEmptyState` |
| 3.7 | `mobile-page-with-nav` on browse pages |
| 3.8 | Add-artist / add-genre / add-song inline guidance |

---

## Tier 4 — Structural page splits (done)

**Goal:** Slim oversized mobile pages into orchestration shells + focused components.

| Item | Delivered |
|------|-----------|
| 4.1 My Songs | `MySongsSortToolbar`, `MySongsGroupedList`, `GroupedPagingState` |
| 4.2 Log picker | `LogSongPickerPanel`, `LogCatalogState` |
| 4.3 Song detail | `SongDetailFirstLog`, `SongDetailWithHistory` |
| 4.4 Invite share | `InviteSharePanel`, `InviteShareLoader` |
| 4.5 Singer shell | `SingerGatedPage` on Log, My Songs, song detail, My performances, My stats |

Admin `/performances` intentionally still uses `SingerProfileGate` directly (desktop grid layout).

---

## Tier 5 — Admin catalog (planned)

**Goal:** Higher-impact refactors for **More → Catalog** admin grids — different audience from mobile singer pages, but shared patterns reduce duplication and silent failures.

| # | Item | Effort | Value |
|---|------|--------|-------|
| **5.1** | **Admin grid error handling** — try/catch or `TryCreate*` / `TryUpdate*` / `TryDelete*` in `OnActionBegin` | Small–medium | Prevents silent crashes on Genres, Artists, Singers, Venues, Songs, Performances |
| **5.2** | **`CatalogCrudGrid<T>`** — parameterized Syncfusion grid for Genres / Singers / Venues (+ Artists) | Medium | ~200 lines removed across near-identical pages |
| **5.3** | **Admin performance edit → `PerformanceEditOperations`** | Medium | Single edit/delete behavior between admin `/performances` grid and mobile lists |
| **5.4** | **`SongDisplayMapper`** — extract FK mapping from `Songs.razor` | Small | Testable admin helper for artist/genre display IDs |
| **5.5** | **bUnit smoke tests** — one test per admin page with mocked API | Medium | Safety net when grids or `OnActionBegin` handlers change |
| **5.6** | **Shared `NotImplementedApiClient` test double** | Small | Stops repeated full-interface stubs in loader/sync tests from breaking on new `IKaraokeApiClient` members |

**Exit criteria:** Admin catalog pages share grid CRUD/error patterns; admin smoke tests pass; test doubles centralized.

**Primary files:** `Pages/Genres.razor`, `Artists.razor`, `Singers.razor`, `Venues.razor`, `Songs.razor`, `Performances.razor`; `Services/PerformanceEditOperations.cs`; `KaraokeList.Web.Tests/`.

---

## Tier 6 — Infrastructure (optional / when scaling)

**Goal:** Low-urgency structural improvements — tackle individually when pain appears, not as a single batch.

| # | Item | When |
|---|------|------|
| **6.1** | Move `IsOfflineFailure` to `ApiTransientFailure` | Anytime — trivial; deduplicates offline detection in `LogCatalogLoader`, `MySongsLoader`, etc. |
| **6.2** | Segment `IKaraokeApiClient` (admin / singer / auth) | When interface churn hurts — fewer stub surfaces per test |
| **6.3** | Generic `ApiResult<T>` instead of many result types | Large refactor; low urgency |
| **6.4** | UrlAdaptor + server paging for catalog grids | When catalog size grows beyond in-memory grid loads |
| **6.5** | Split `QuickLogPerformance` into form + save service | If log flow keeps growing after mobile tiers |

---

## Open feature backlog (not tiered)

These are tracked issues outside the refactor tier sequence — ship independently when prioritized:

| Issue | Topic |
|-------|--------|
| [#92](https://github.com/jpridx/KaraokeList/issues/92) | More flexible search (punctuation / apostrophe tolerance) |
| [#62](https://github.com/jpridx/KaraokeList/issues/62) | Genre groups (broad categories, multi-membership) |
| [#57](https://github.com/jpridx/KaraokeList/issues/57) | Excel catalog upload |

---

## Working conventions

- **Branches:** `cursor/<short-description>-2e87` off `master`; PR into `master`.
- **Issues:** Put `Closes #nn` in the **PR body** (not just commit messages) for auto-close on squash merge.
- **Verification:** `dotnet build`; `dotnet test KaraokeList.Web.Tests/KaraokeList.Web.Tests.csproj`; run API + WASM for manual mobile flows; admin grid smoke tests when touching Tier 5.
- **Docs:** Update this file when a tier item ships; add new components to [mobile-ux.md](mobile-ux.md).

## Related docs

| Doc | Role |
|-----|------|
| [mobile-ux.md](mobile-ux.md) | Routes, venue-night flows, shared components |
| [Performances.md](Performances.md) | API + performance schema |
| [e2e-playwright.md](e2e-playwright.md) | Playwright setup and run commands |
| [deployment-roadmap.md](deployment-roadmap.md) | Production hosting (separate from UI refactor tiers) |
