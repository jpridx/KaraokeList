# AI integration options

Design notes and candidate features for adding AI/LLM integration to KaraokeList. Captured for discussion — **not implemented yet**.

Related open issues: [#57](https://github.com/jpridx/KaraokeList/issues/57) · [#62](https://github.com/jpridx/KaraokeList/issues/62) · [#92](https://github.com/jpridx/KaraokeList/issues/92)

---

## Guiding principles

- **Low token cost.** AI calls should fire on explicit user action, not on every keystroke or page load. A call that sends ~100 tokens and returns a single classification is the target shape.
- **User stays in control.** Every AI suggestion is optional and overridable. No silent AI behavior that changes data without confirmation.
- **Backend owns the call.** The API holds the LLM API key and makes the network call. The WASM client only sends a request to a KaraokeList endpoint and displays the result. This keeps secrets off the client and makes the feature easy to disable.
- **Graceful degradation.** If the AI call fails or is unconfigured, the UI falls back silently to manual input. No hard dependency on AI availability.

---

## Architecture pattern (shared by all candidates)

```
Singer (WASM)
    │  POST /api/ai/<action>  { title, artist, … }
    ▼
KaraokeList.Api  →  LLM provider (OpenAI, Anthropic, etc.)
                          ↓
                  structured response (one field, JSON)
    │
    ▼
WASM pre-fills / suggests; user confirms or overrides
```

**API key management:** `IConfiguration` key (e.g. `Ai:OpenAiKey`). Dev: `dotnet user-secrets`; Azure: App Service environment variable. Key is never sent to the client.

**NuGet candidates:** `Azure.AI.OpenAI` (first-party, well-maintained) or `Anthropic.SDK`. Either wraps a single HTTP call — no heavy framework required for these use cases.

---

## Candidate 1 — Genre auto-suggestion when adding a song

**Status:** Under discussion
**Related issues:** daily workflow / song add (no dedicated issue yet)

### Problem

When a singer adds a new song at the venue (or an admin enters one via the catalog grid), they must manually pick a genre. The wrong pick — or leaving it blank — degrades genre-filter UX across My Songs and My Stats for everyone who logs that song.

### AI hook

```
POST api/ai/suggest-genre
{ "title": "Don't Stop Believin'", "artist": "Journey" }

→ { "suggestedGenreId": 4, "suggestedGenreName": "Classic Rock", "confidence": "high" }
```

Prompt shape (sent server-side):

> What is the best genre for the song "{Title}" by "{Artist}"?  
> Choose exactly one genre from this list: {comma-separated genre names from DB}.  
> Respond with only the genre name, nothing else.

The API resolves the returned name to a `GenreId` from the existing genres table and returns the DTO. If the name does not match any genre, the API returns no suggestion. Token cost: ~60–100 per call.

### UI integration

- **`AddGenreField.razor`** (used in `AddSongPanel`) — add a small "Suggest ✨" link next to the genre dropdown. Fires when Title and Artist are both filled. Pre-selects the genre in the dropdown; user can change it.
- The suggestion only fires on explicit click, never automatically, to keep token use predictable.

### Scope of changes

| Area | Change |
|------|--------|
| `KaraokeList.Shared` | `GenreSuggestionRequest` / `GenreSuggestionResponse` DTOs |
| `KaraokeList.Api` | New `AiController.cs` (or method on `GenresController`) with `POST api/ai/suggest-genre`; `IAiGenreService` wrapping the LLM call |
| `KaraokeList.Api/appsettings.json` | `"Ai": { "OpenAiKey": "" }` placeholder |
| `KaraokeList.Web` | `KaraokeApiClient` method; UI addition in `AddGenreField.razor` |

### Questions before implementation

1. **LLM provider preference** — OpenAI (GPT-4o mini is cheap and fast) or Anthropic Claude Haiku? Both have similar C# SDK quality and pricing for single-classification calls.
2. **Trigger: button click vs. auto-fire after a debounce?** Button click is simpler and more predictable for token budgeting. Auto-fire after 1s of inactivity (when both fields are filled) is more magical but harder to control.
3. **Fallback behavior when AI key is not configured** — hide the button entirely, or show it grayed out with a tooltip?
4. **Should the feature be admin-only, or available to all singers adding songs?** Regular singers can already add songs via `AddSongPanel` at the venue.

---

## Candidate 2 — Excel column auto-detection for bulk import

**Status:** Blocked on base feature; under discussion
**Related issues:** [#57 — Ability to upload songs from an Excel sheet](https://github.com/jpridx/KaraokeList/issues/57)

### Problem

The bulk import feature (#57) does not exist yet. When it is built, users will supply spreadsheets with varying layouts — one file has "Song Title" in column A, another has "Track Name" in column C. Manual column-mapping configuration is tedious.

### AI hook

On upload, send the first 10 rows (headers + sample values, stringified as CSV) to the LLM:

> These are the first rows of a spreadsheet. Map each column to one of these fields: Title, Artist, Genre, Year, SecondaryArtist, or Ignore.  
> Return a JSON object: `{ "columnIndex": "fieldName", … }`.

The user sees a confirmation table of the proposed mapping before any import proceeds. They can adjust any column before confirming. Token cost: ~200–400 per upload (headers + sample rows), fires once per file.

### Scope of changes

This candidate **requires the base Excel import feature to land first** (file upload, parsing, deduplication, catalog-merge hooks from the #57 comment). The AI column-detection is a thin layer on top. Suggested implementation order: build #57 with a manual column-mapping UI first, then optionally wire AI auto-detection as an enhancement.

### Questions before implementation

1. Should column mapping be mandatory (user must confirm before import) or optional (skip straight to import if confidence is high)?
2. What file formats should the base import support? Excel (`.xlsx`), CSV, TSV? AI detection is equally useful for all; the parsing layer differs.

---

## Candidate 3 — AI-assisted genre grouping

**Status:** Blocked on data model; under discussion
**Related issues:** [#62 — Genre groups](https://github.com/jpridx/KaraokeList/issues/62)

### Problem

Issue #62 asks for a genre hierarchy so that "Alternative Rock," "Classic Rock," and "Hard Rock" can appear under a "Rock" umbrella. The difficult part is deciding which existing genres belong to which parent group.

### AI hook

An admin-only action: "Suggest genre groups." The API sends the full genre list (currently small — tens of genres) to the LLM:

> Group these music genres into broad umbrella categories. A genre may belong to more than one group.  
> Genres: {comma-separated list}. Return JSON: `[{ "group": "Rock", "genres": ["Classic Rock", "Alternative Rock", …] }, …]`.

The admin reviews the proposed hierarchy in a confirmation UI and can adjust group names or move genres before saving. Token cost: ~200–400 for a typical genre list, fires once per admin action.

### Scope of changes

This candidate **requires the genre-group data model (#62) to be designed first** — a `GenreGroup` table and the schema changes to connect genres to groups. The AI part is a seeding/admin assist on top of that model, not a replacement for it.

### Questions before implementation

1. Should genre groups be a fixed taxonomy (admin edits a seeded list) or fully user-defined? This changes the data model significantly.
2. Can a genre belong to multiple groups (the issue says yes)? Many-to-many adds complexity.
3. Is AI assist needed at all for the initial taxonomy, or is a hand-curated starter list sufficient with AI available later for new genres?

---

## What is NOT a good AI fit

| Issue | Why not AI |
|-------|-----------|
| [#92 — Flexible search](https://github.com/jpridx/KaraokeList/issues/92) | Pure text normalization (`dont` → `don't`). Cheaper, faster, and more deterministic with a `FlexibleSearch.Normalize()` helper in `KaraokeList.Shared`. See [flexible-search-options.md](flexible-search-options.md). |
| [#127–#129 — Spotify / iTunes / YouTube Music links](https://github.com/jpridx/KaraokeList/issues/127) | Deep-link URL construction: `https://open.spotify.com/search/{title}+{artist}`. No AI needed. |
| [#117 — Better loading notifications](https://github.com/jpridx/KaraokeList/issues/117) | UI/UX skeleton/progress pattern. No AI needed. |

---

## Status

| Date | Note |
|------|------|
| 2026-07-01 | Candidates documented for discussion; no code changes |

When a candidate is approved for implementation, update this file (mark chosen option, add chosen provider/trigger), update [mobile-refactor-roadmap.md](mobile-refactor-roadmap.md), and close any relevant issue via PR body `Closes #nn`.
