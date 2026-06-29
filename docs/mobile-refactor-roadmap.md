# Mobile refactor roadmap

Planning doc for the **architecture review** that started from [#70](https://github.com/jpridx/KaraokeList/issues/70) (componentize controls) and [#75](https://github.com/jpridx/KaraokeList/issues/75) (AddSongPanel flow polish). Tiers are ordered by **refactor ROI** — high-duplication extractions first, large page splits next, then admin/catalog alignment, then user-facing capabilities.

Primary scope: **KaraokeList.Web** mobile singer flows. Catalog admin grids and API changes are included when they unblock mobile UX or shared patterns.

See [mobile-ux.md](mobile-ux.md) for routes, flows, and the shared-components table.

## Status overview

| Tier | Theme | Status |
|------|--------|--------|
| **1** | High-ROI shared components | **Done** — PR [#98](https://github.com/jpridx/KaraokeList/pull/98) |
| **2** | Browse toolbars + performance edit helpers | **Done** — PR [#99](https://github.com/jpridx/KaraokeList/pull/99) (+ follow-up commits on same branch) |
| **3** | Mobile polish (shells, empty states, guidance) | **Done** — PR [#103](https://github.com/jpridx/KaraokeList/pull/103) |
| **4** | Structural page splits | **Done** — PR [#106](https://github.com/jpridx/KaraokeList/pull/106) |
| **4.5** | `SingerGatedPage` wrapper | **Done** — PR [#108](https://github.com/jpridx/KaraokeList/pull/108) |
| **5** | Remaining structural refactors | **Planned** |
| **6** | Capabilities & end-to-end confidence | **Planned** |

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

## Tier 5 — Remaining structural refactors (planned)

**Goal:** Finish component extraction on the largest remaining Razor files and align admin/catalog pages with shared patterns. No new user-facing features in this tier.

| Item | Target | Notes |
|------|--------|-------|
| **5.1** `QuickLogPerformance` split | `QuickLogPerformance.razor` (~330 lines) | Extract venue picker (incl. new venue), date/key panel, and save/offline orchestration into focused child components or a small state helper. Keep `CoPerformersEditor` + `HostMessagePanel` wiring in the parent or a thin coordinator. |
| **5.2** Log + My Songs shells | `Log.razor`, `MySongs.razor` | Move list/filter orchestration and code-behind into dedicated state loaders or panels (e.g. `MySongsBrowseState`, `LogPageCoordinator`) so pages are mostly composition. |
| **5.3** Admin Performances | `Performances.razor` | Share more markup with mobile patterns where it does not fight the Syncfusion grid — e.g. summary panel layout, singer gate messaging, consistent `StatusAlerts`. Do **not** force `SingerGatedPage` if the desktop grid layout suffers. |
| **5.4** Catalog grid patterns | `/songs`, `/artists`, `/genres`, … | Identify repeated grid toolbar / dialog / validation patterns; extract shared fragments or base helpers. Prefer `UrlAdaptor` + API endpoints over duplicated client-side grid config. |
| **5.5** Component test hardening | `KaraokeList.Web.Tests` | bUnit coverage for Tier 5 extractions and binding edge cases (literal `@` params, custom-component `:after` callbacks). Regression tests for Log field visibility and PWA banner contrast. |

**Exit criteria:** No mobile page shell over ~150 lines of mixed markup + logic without a documented reason; new extractions listed in [mobile-ux.md](mobile-ux.md) shared-components table.

---

## Tier 6 — Capabilities & end-to-end confidence (planned)

**Goal:** After structure is stable, ship singer-visible improvements and prove core venue-night flows in the browser. Tier 6 mixes **features**, **API support**, and **Playwright E2E** — discuss scope per item before large Syncfusion workarounds.

| Item | Target | Notes |
|------|--------|-------|
| **6.1** Flexible search | [#92](https://github.com/jpridx/KaraokeList/issues/92) | Tolerate punctuation/apostrophe differences in mobile browse search (e.g. `dont` ↔ `don't`). Prefer server-side or shared normalizer over per-control hacks; evaluate Syncfusion filter limitations early. |
| **6.2** Genre groups | [#62](https://github.com/jpridx/KaraokeList/issues/62) | Broad categories (e.g. rock, country) for grouping/filtering; genres may belong to multiple groups. Touches catalog schema/API and My Songs genre chips / grouped view. |
| **6.3** Excel catalog upload | [#57](https://github.com/jpridx/KaraokeList/issues/57) | Admin bulk import from spreadsheet — API validation, duplicate handling, WASM upload UX under **More → Catalog**. |
| **6.4** Playwright mobile journeys | `KaraokeList.E2E` | End-to-end coverage for: log performance (incl. pre-selected song), My Songs browse + detail, offline queue + sync banner, register/login smoke. See [e2e-playwright.md](e2e-playwright.md). |
| **6.5** Catalog query API | `KaraokeList.Api` | Search/filter endpoints that support 6.1–6.3 without pushing complex logic into WASM or Syncfusion client filters — e.g. normalized text search, genre-group membership, import staging. |

**Exit criteria:** Open issues #92, #62, #57 addressed or explicitly deferred with documented tradeoffs; E2E suite runs in CI for the core mobile path documented in [mobile-ux.md](mobile-ux.md).

---

## Working conventions

- **Branches:** `cursor/<short-description>-2e87` off `master`; PR into `master`.
- **Issues:** Put `Closes #nn` in the **PR body** (not just commit messages) for auto-close on squash merge.
- **Verification:** `dotnet build`; `dotnet test KaraokeList.Web.Tests/KaraokeList.Web.Tests.csproj`; run API + WASM for manual mobile flows; E2E when touching Tier 6.4.
- **Docs:** Update this file when a tier item ships; add new components to [mobile-ux.md](mobile-ux.md).

## Related docs

| Doc | Role |
|-----|------|
| [mobile-ux.md](mobile-ux.md) | Routes, venue-night flows, shared components |
| [Performances.md](Performances.md) | API + performance schema |
| [e2e-playwright.md](e2e-playwright.md) | Playwright setup and run commands |
| [deployment-roadmap.md](deployment-roadmap.md) | Production hosting (separate from UI refactor tiers) |
