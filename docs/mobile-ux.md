# Mobile UX (Blazor WASM)

Phone-first flows for logging performances at a venue and browsing songs you've sung. Desktop catalog admin still uses Syncfusion grids under **More → Catalog**.

## Projects

| Project | Role |
|---------|------|
| `KaraokeList.Web` | Blazor WebAssembly UI (primary for singers) |
| `KaraokeList.Api` | JWT auth + SQL catalog/performance API |
| `KaraokeList` | Legacy Blazor Server app (reference; grids and Identity demos) |

Run locally: see [wasm-api-local-dev.md](wasm-api-local-dev.md).

## Navigation

**Phone (< 768px)**

- Top bar: KaraokeList + Sign out
- Bottom bar: **Log** · **My Songs** · **More**

**Desktop**

- Top bar: KaraokeList · Log · My Songs · Catalog (→ `/more`) · Sign out

There is no sidebar. Catalog grids live under `/more`.

## Routes (authenticated unless noted)

| Route | Purpose |
|-------|---------|
| `/log` | Pick a song (catalog ComboBox), log venue/date/key, copy for host, save |
| `/log?songId={id}` | Same, with song pre-selected (from My Songs **Log** button) |
| `/my-songs` | Browse your repertoire: search, sort, genre filter, group-by-genre |
| `/my-songs/{id}` | Log again (quick form) + collapsible performance history |
| `/more` | Hub: tonight links + catalog admin pages |
| `/` | Home (links to mobile flows when signed in) |
| `/login`, `/register` | Auth (no sign-in required) |

### Catalog (desktop-oriented grids)

`/songs`, `/artists`, `/genres`, `/singers`, `/venues`, `/performances` — Syncfusion grids for bulk edit. Reachable from **More → Catalog**.

## Typical night at the venue

### Log a song you already know

1. Open **My Songs**
2. Search or scroll
3. Tap **Log** on the row → `/log?songId=…` with song selected
4. Confirm venue, date (defaults to today), key (defaults to last time)
5. **Copy for host** → paste to the KJ
6. **Save performance**

### Log from the full catalog

1. Open **Log**
2. Search songs (★ = you've sung it before)
3. Same venue/date/key/copy/save flow

### Browse + history

1. Tap the **song row** (not Log) → `/my-songs/{id}`
2. Use **Log again** at the top (same quick form as Log)
3. Expand **Performance history** for past dates/venues/keys

## Copy for host

Formatted message for the show runner:

| Key | Example |
|-----|---------|
| Original | `Jeopardy - The Greg Kihn Band` |
| Down 2 | `Jeopardy - The Greg Kihn Band (Down 2)` |
| Up 1 | `Sweet Caroline - Neil Diamond (Up 1)` |

Implemented in `ShowHostMessageFormatting` (`KaraokeList.Shared`). Shown on Log and song detail via `HostMessagePanel` (**Copy for host** copies to clipboard).

## Shared components

| Component | Role |
|-----------|------|
| `QuickLogPerformance` | Venue, date, key, host message, save — defaults from last performance |
| `HostMessagePanel` | Preview + copy button |
| `SongListItem` | Repertoire row: tap body = history, **Log** = quick log |
| `SingerLinkPanel` | Link login to a singer profile when `SingerId` is missing |

## API used by mobile pages

See [Performances.md](Performances.md) for schema and endpoints. Key calls:

- `GET api/performances/my-repertoire` — My Songs list
- `GET api/performances/my-song-summary?songId=` — defaults + history
- `POST api/performances` — save (singer from JWT if omitted)
- `GET api/auth/me` — singer link status

## Auth

- Register/login via API; JWT in browser local storage
- Registration creates a singer and sets `AspNetUsers.SingerId`
- Mobile features need a linked singer (`SingerLinkPanel` if not)

See [security-private-access.md](security-private-access.md) for invite codes and deployment hardening.
