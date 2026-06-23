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
- Bottom bar: **Log** · **My Songs** · **Performances** (≥400px wide) · **More**
  - On very narrow phones (&lt;400px), **Performances** hides from the bar — use **More → My performances**
  - Labels shorten to **Songs** / **Performs** until the screen is wide enough for full text

**Desktop**

- Top bar: KaraokeList · Log · My Songs · Catalog (→ `/more`) · Sign out

There is no sidebar. Catalog grids live under `/more`.

## Routes (authenticated unless noted)

| Route | Purpose |
|-------|---------|
| `/log` | Pick a song (catalog ComboBox), log venue/date/key, copy for host, save |
| `/log?songId={id}` | Same, with song pre-selected (from My Songs **Log** button) |
| `/my-songs` | Browse the full catalog (default) or filter logged / not logged; search, sort, genre; virtualized scroll (flat list) or load-more when grouped |
| `/my-songs/{id}` | Stats, copy for host, log again (collapsible), performance history |
| `/my-performances` | Chronological performance list; search, venue filter, edit/delete |
| `/more` | Hub: tonight links + catalog admin pages |
| `/invite-friends` | Copy registration link / invite message for text or email (when invite-only registration is enabled) |
| `/` | Home — **Tonight** dashboard on mobile (recent logs, default venue, quick actions) |
| `/login`, `/register` | Auth (no sign-in required) |

### Catalog (desktop-oriented grids)

`/songs`, `/artists`, `/genres`, `/singers`, `/venues`, `/performances` — Syncfusion grids for bulk edit. Reachable from **More → Catalog**.

## Typical night at the venue

### Log a song you already know

1. Open **My Songs**
2. Search or scroll
3. Tap **Log** on the row → `/log?songId=…` with song selected
4. Confirm venue, date (defaults to today), key (defaults to **last key for this song**, or original key if you've never logged it)
5. **Copy for host** → paste to the KJ
6. **Save performance**

### Log from the full catalog

1. Open **Log**
2. Search songs (★ = you've sung it before)
3. Same venue/date/key/copy/save flow (key is per song, not carried over from the previous log)

### Add a new song at the venue

1. Open **Log** → **+ New song**
2. Enter the **title**, then type the **artist** name (autocomplete search)
3. If the artist isn't in the catalog, tap **Add artist** — your title stays filled in
4. Tap **Add song** — the new song is selected and the log form appears below
5. Pick venue, date, key, and **Save performance**

Use **Cancel** to close the add-song panel without losing your place on the Log page.

### Browse the full catalog

1. Open **My Songs** (defaults to **All songs**)
2. Use **Logged** / **Not logged** to focus on songs with or without recorded performances
3. Songs without a logged performance show *Not logged* — tap to open detail and log one

### Browse + history

1. Tap the **song row** (not Log) → `/my-songs/{id}`
2. Review **times sung** and last performance at the top; **Copy for host** uses your last key
3. Expand **Performance history** to edit or delete past rows
4. Expand **Log again** when you're ready to record another performance
5. For songs you've never logged, the log form is shown upfront with a short empty-state hint

### Browse all performances

1. Open **My performances** from the bottom bar (when shown) or **More → My performances**
2. Search by song, artist, or venue; filter by venue chip; toggle newest/oldest
3. Tap a row for song detail, **Log** to log again, or **Edit** / **Delete** inline
4. Use **Load more** for long histories

## Offline-tolerant Log

When the API is slow or unreachable (spotty venue Wi‑Fi, database cold start), **Log performance** still works:

1. **Cache first:** Open **Log** once while online. The app caches the song catalog, your repertoire markers (★), and venues on this device.
2. **Pick a song offline:**
   - Search the cached catalog in the song combobox, or
   - Tap a row under **Recently logged**, or
   - Open **Log** from **Tonight** / **My Songs** with a song pre-selected (`/log?songId=…`).
3. Fill in venue, date, and key. Pick an **existing venue** — adding new venues still requires the server.
4. Tap **Save performance**. If the server cannot be reached, the app saves on this device: *Saved on this device. Will sync when you're back online.*
5. A yellow banner shows pending sync count with **Sync now**. The app retries automatically on navigation when connectivity returns.

If you go offline before ever opening Log online, use **Recently logged** to pick songs until the catalog is cached.

**Not available offline:** adding songs/artists/venues, My Songs browse, or performance history edits.

## Tonight dashboard (mobile home)

Signed-in phone users land on **Tonight** at `/`:

- Today's date
- Default venue from your last log (when set)
- Up to three recently logged songs (tap to open Log with that song)
- **Log performance** and **My Songs** shortcuts

## Install as an app (PWA)

On HTTPS (production), use the browser **Add to Home Screen** / **Install app** option. KaraokeList ships a web manifest and service worker so it opens full-screen like a native app at the venue.

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
| `QuickLogPerformance` | Venue, date, key, host message, save — venue/date from last log; key from last time **you sang this song** (or original key); queues offline |
| `LogNewSongPanel` | Add song + artist inline on Log — autocomplete artist, explicit **Add artist** when missing, title preserved |
| `LogCatalogLoader` | Online catalog fetch + offline cache for Log song/venue picker |
| `PendingPerformancesNotice` | Pending sync banner + auto/manual sync |
| `HostMessagePanel` | Preview + copy button |
| `SongListItem` | Repertoire row: tap body = history, **Log** = quick log |
| `PerformanceBrowseList` | Mobile performance browse rows with edit/delete |
| `PerformanceHistoryList` | Editable performance history on song detail |
| `TonightDashboard` | Mobile home: tonight context + recent logs |
| `SingerLinkPanel` | Link login to a singer profile when `SingerId` is missing |

## Invite friends

When production registration requires an invite code, signed-in users open **More → Invite friends** to copy:

- A **registration link** with `?invite=` pre-filled (`InviteShareFormatting` in `KaraokeList.Shared`)
- A **full message** suitable for text or email

The API exposes `GET api/auth/invite-share` (JWT required); it returns the configured invite code only when registration is open and invite-only mode is enabled. See [security-private-access.md](security-private-access.md).

## API used by mobile pages

See [Performances.md](Performances.md) for schema and endpoints. Key calls:

- `GET api/performances/my-repertoire` — My Songs list
- `GET api/performances/my-history` — mobile performance browse
- `GET api/performances/my-song-summary?songId=` — defaults + history
- `POST api/performances` — save (singer from JWT if omitted)
- `GET api/auth/me` — singer link status
- `GET api/auth/invite-share` — invite link/message payload for signed-in users (when invite-only registration is configured)

## Auth

- Register/login via API; JWT in browser local storage
- Registration creates a singer and sets `AspNetUsers.SingerId`
- Mobile features need a linked singer (`SingerLinkPanel` if not)

See [security-private-access.md](security-private-access.md) for invite codes and deployment hardening.
