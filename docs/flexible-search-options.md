# Flexible search options ([#92](https://github.com/jpridx/KaraokeList/issues/92))

Design notes for **more flexible search** — punctuation, apostrophes, and whitespace should not make matching overly brittle (e.g. `dont` should find *Don't Stop Believin'*). Captured for later review; **not implemented yet**.

Related: [mobile-refactor-roadmap.md](mobile-refactor-roadmap.md) (open feature backlog).

## Problem statement (from #92)

> Right now, punctuation and whitespace make search a very "particular" exercise. A more flexible search (e.g., letting dont match don't) would greatly improve the UX. This might be a limitation of the Syncfusion control — I don't want to automatically add too much infrastructure to work around such limitations if they exist. I would prefer to discuss.

**Goal:** Improve singer-facing search without over-engineering or large API/schema work unless catalog scale forces it.

---

## Where search happens today

| Area | Mechanism | Normalization today |
|------|-----------|---------------------|
| **My Songs** | `RepertoireSearch` (`KaraokeList.Shared`) | Trim query; case-insensitive `Contains` on title, artist, genre |
| **My performances** | `MyPerformancesSearch` (`KaraokeList.Shared`) | Same pattern on title, artist, venue |
| **Log song picker**, artist/venue dropdowns, admin grids | Syncfusion `FilterType.Contains` | Literal substring in the control |

Mobile browse helpers are simple and unit-tested (`RepertoireSearchTests`, `MyPerformancesSearchTests`). Tests include *Don't Stop Believin'* in sample data but do **not** yet assert `dont` ↔ `don't`.

Syncfusion combobox/grid filtering is separate — changing `RepertoireSearch` alone does **not** fix Log or admin catalog search.

---

## Options (smallest → largest)

### Option A — Shared text normalizer + mobile browse only

**Effort:** Small · **Risk:** Low · **Syncfusion:** Untouched

Add e.g. `FlexibleSearch.Normalize()` in `KaraokeList.Shared`:

- Lowercase
- Strip apostrophes / curly quotes (`'`, `'`, `` ` ``)
- Strip or ignore common punctuation (`.`, `-`, `&`, `!`, etc.)
- Collapse runs of whitespace

Use in `RepertoireSearch.Matches` and `MyPerformancesSearch.Matches` — compare normalized query against normalized title/artist/genre (or venue).

| Pros | Cons |
|------|------|
| Fixes main mobile browse pain | Does not fix Log combobox or admin grids |
| Easy unit tests | Rules need product agreement (see below) |
| No UI / Syncfusion changes | |

**Suggested first step** — matches #92 preference to discuss before big infrastructure.

---

### Option B — Option A + token matching (multi-word)

**Effort:** Small+ · Same scope as A

Require **each word** in the query to match somewhere in the row (title, artist, genre/venue):

- `dont believin` → *Don't Stop Believin'*
- `greg kihn` → *Jeopardy - The Greg Kihn Band*

Still client-side; still My Songs / My performances unless combined with Option C.

---

### Option C — Custom Syncfusion filtering (Log + dropdowns)

**Effort:** Medium · **Risk:** Medium · **Syncfusion:** Local workaround

Syncfusion ComboBox supports a **`Filtering` event** (`PreventDefaultAction` + custom LINQ or `FilterAsync`). Plan:

1. Reuse `FlexibleSearch` from Option A
2. Wire into `CatalogSongPicker` (Log + **Add from catalog** on list panels)
3. Optionally `AddArtistField`, venue pickers later (smaller lists, lower priority)

| Pros | Cons |
|------|------|
| Fixes high-value venue-night song pick | Per-control wiring |
| Reuses same rules as mobile browse | Syncfusion-specific; trickier to test |
| Not a server/API redesign | |

---

### Option D — Normalize at load time (`SearchKey` on items)

**Effort:** Medium · **Value:** Limited alone

Precompute a `SearchKey` on `LogSongPickItem` (and similar) when catalog loads.

**Caveat:** Syncfusion still filters the bound `Display` field unless Option C is also done. Normalizing at load without custom filtering does **not** fix the combobox by itself. Useful paired with C, not standalone.

---

### Option E — Server-side normalized search (API / SQL)

**Effort:** Large · **Risk:** Higher · See roadmap Tier 6.4

Normalized columns or SQL expressions; search query params on repertoire/history/catalog endpoints.

| Pros | Cons |
|------|------|
| Scales for huge catalogs | Schema/query complexity |
| Single source of truth | Overkill while lists are fully loaded in WASM |

Revisit with **UrlAdaptor + server paging** when in-memory catalog grids are no longer viable — not required for #92 alone.

---

### Option F — Fuzzy / typo tolerance (Levenshtein, etc.)

**Effort:** Medium–large · **Scope:** Probably out of scope for #92

`dont` vs `don't` is **normalization**, not fuzzy matching. Typo forgiveness (`jeapordy` → `jeopardy`) is a different feature with more false positives. Defer unless explicitly requested.

---

## Recommended phasing

| Phase | What | Fixes |
|-------|------|--------|
| **1** | **A** (optionally **B**) | My Songs, My performances |
| **2** | **C** on `CatalogSongPicker` | Log, Add from catalog |
| **Later** | C on artist/venue dropdowns | If still annoying |
| **Defer** | Admin grid FilterBar, Option E | Desktop admin; scale |

Delivers most singer UX with a small testable change in Shared; touches Syncfusion only where it hurts most.

---

## Normalization rules to decide before implementation

| Rule | Example | Suggested default |
|------|---------|-------------------|
| Apostrophes | `don't` ↔ `dont` | Yes — treat as equivalent |
| Hyphens | `semi-charmed` ↔ `semi charmed` | Product call (lean yes) |
| Accents | `café` ↔ `cafe` | Optional phase 1 (Unicode NFD strip) |
| Other punctuation | `Mr. Brightside` ↔ `mr brightside` | Strip common punctuation |
| Match style | Substring vs all tokens match | Phase 1: normalized substring; phase 2: optional tokens (B) |

Document chosen rules in unit tests so behavior stays explicit.

---

## What to avoid (per #92)

- Jumping straight to Option E (new API endpoints / schema) while lists still filter client-side
- A generic “search framework” wired everywhere in one PR
- Replacing Syncfusion pickers with custom autocomplete unless Option C proves insufficient

---

## Implementation sketch (when approved)

**Phase 1**

```
KaraokeList.Shared/FlexibleSearch.cs     — Normalize(), Matches(haystack, needle), optional MatchesAllTokens()
KaraokeList.Shared/RepertoireSearch.cs   — use FlexibleSearch
KaraokeList.Shared/MyPerformancesSearch.cs
KaraokeList.Api.Tests/ or Web.Tests/     — dont → Don't Stop Believin', hyphen/punctuation cases
```

**Phase 2**

```
KaraokeList.Web/Components/CatalogSongPicker.razor — Filtering event + FlexibleSearch over Items
(Optional helper) FlexibleComboBoxFilter.cs
```

**Verification:** `dotnet test` on new `FlexibleSearchTests`; manual Log + My Songs on a song with apostrophe in title.

---

## Status

| Date | Note |
|------|------|
| 2026-06-29 | Options documented for review; no code changes yet |

When implemented, update this file (mark chosen options + phase), [mobile-refactor-roadmap.md](mobile-refactor-roadmap.md), and close #92 via PR body `Closes #92`.
