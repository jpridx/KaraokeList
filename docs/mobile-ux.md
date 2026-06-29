# Mobile UX (Blazor WASM)

Phone-first flows for logging performances at a venue and browsing songs you've sung. Desktop catalog admin still uses Syncfusion grids under **More → Catalog**.

Refactor planning (Tiers 1–6): [mobile-refactor-roadmap.md](mobile-refactor-roadmap.md).

## Projects

| Project | Role |
|---------|------|
| `KaraokeList.Web` | Blazor WebAssembly UI (primary for singers) |
| `KaraokeList.Api` | JWT auth + SQL catalog/performance API |

Run locally: see [wasm-api-local-dev.md](wasm-api-local-dev.md).

## Navigation

**Phone (< 768px)**

- Top bar: KaraokeList + Sign out
- Bottom bar: **Log** · **My Songs** · **Performances** (≥400px wide) · **More**
  - On very narrow phones (&lt;400px), **Performances** hides from the bar — use **More → My performances**
  - Labels shorten to **Songs** / **Performs** until the screen is wide enough for full text

**Desktop**

- Top bar: KaraokeList · Log · My Songs · More · Sign out
- Admins also see **Users** on desktop; catalog grids under **More → Catalog**

There is no sidebar. Catalog grids live under `/more`.

## Routes (authenticated unless noted)

| Route | Purpose |
|-------|---------|
| `/log` | Pick a song (catalog ComboBox), log venue/date/key, copy for host, save |
| `/log?songId={id}` | Same, with song pre-selected (from My Songs **Log** button) |
| `/my-songs` | Browse **My repertoire**, **Want to sing**, or **Working up** lists; search, sort, genre; virtualized scroll (flat list) or load-more when grouped |
| `/my-songs/{id}` | Stats, copy for host, log again (collapsible), performance history |
| `/my-performances` | Chronological performance list; search, venue filter, edit/delete |
| `/my-stats` | Full stats: totals, top venues/songs/artists, new repertoire |
| `/more` | Hub: tonight links + catalog admin pages |
| `/invite-friends` | Copy registration link / invite message for text or email (when invite-only registration is enabled) |
| `/account/change-password` | Change sign-in password |
| `/account/preferences` | Preferences for **Haven't sung in a while** (days and song limit) |
| `/forgot-password` | Request password reset email (when recovery is enabled) |
| `/reset-password` | Set a new password from email reset link |
| `/` | Home — **Tonight** dashboard on mobile; stats teaser; invite banner; stale songs |
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
3. If the artist isn't in the catalog, type their name and tap **Add artist** — your title stays filled in
4. Tap **Add song** — the new song is selected and the log form appears below
5. Pick venue, date, key, and **Save performance**

Use **Cancel** to close the add-song panel without losing your place on the Log page.

### Browse your lists

1. Open **My Songs** (defaults to **My repertoire**)
2. Switch to **Want to sing** or **Working up** to browse other lists
3. Performance count and last-performed date still appear when you have logged that song
4. Tap a row for detail/history (list actions on detail), or **Log** to record a performance
5. On **Want to sing** / **Working up**, use **+ Add from catalog** or **+ New song** (both shown together) to add to the list

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

1. **Cache first:** Open **Log** once while online. The app caches the song catalog, your repertoire markers (★), working-up markers (🎯), and venues on this device. Open **My Songs** once while online to cache your lists for read-only browse offline.
2. **Pick a song offline:**
   - Search the cached catalog in the song combobox, or
   - Tap a row under **Recently logged**, or
   - Open **Log** from **Tonight** / **My Songs** with a song pre-selected (`/log?songId=…`).
3. Fill in venue, date, and key. Pick an **existing venue** — adding new venues still requires the server.
4. Tap **Save performance**. If the server cannot be reached, the app saves on this device: *Saved on this device. Will sync when you're back online.*
5. A yellow banner shows pending sync count with **Sync now**. The app retries automatically on navigation when connectivity returns.

If you go offline before ever opening Log online, use **Recently logged** to pick songs until the catalog is cached.

**Not available offline:** adding songs/artists/venues, editing list membership, or performance history edits. **My Songs** works read-only from cache (browse, search, open song detail links may fail without API).

## Tonight dashboard (mobile home)

Signed-in phone users land on **Tonight** at `/`:

- Today's date
- Default venue from your last log (when set)
- Up to three recently logged songs (tap to open Log with that song)
- **Log performance** and **My Songs** shortcuts

## Install as an app (PWA)

On HTTPS (production), use the browser **Add to Home Screen** / **Install app** option. KaraokeList ships a web manifest and service worker so it opens full-screen like a native app at the venue.

When a new version is deployed while you still have the app open, a blue **Refresh now** banner appears at the top of the screen. Tap it to load the update — you no longer need to force-quit the app. Pending offline performance logs remain on the device until they sync.

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
| `QuickLogPerformance` | Venue, date, key, host message, save — venue from last log; date defaults to today; key from last time **you sang this song** (or original key); queues offline |
| `AddSongPanel` | Add catalog song inline — title field + `AddArtistField` + `AddGenreField`; fires `SongAddedEventArgs` on success |
| `AddSongToListPanel` | Want to sing / Working up — **Add from catalog** and **New song** in one step; success message after add |
| `AddArtistField` | Artist autocomplete (`AllowCustom`) with inline guidance and **Add artist** button when the typed name isn't in the catalog |
| `AddGenreField` | Genre autocomplete (`AllowCustom`) with inline **Add genre** button when the typed name isn't in the catalog |
| `LogCatalogLoader` | Online catalog fetch + offline cache for Log song/venue picker (★ repertoire, 🎯 working up) |
| `MySongsLoader` | Online list fetch + offline cache for My Songs browse |
| `PendingPerformancesNotice` | Pending sync banner + auto/manual sync |
| `AppUpdateNotice` | PWA update banner when a new app version is waiting |
| `HostMessagePanel` | Preview + copy button |
| `SongListItem` | Repertoire row: tap body = history, **Log** = quick log |
| `PerformanceBrowseList` | *(removed — use `EditablePerformanceList` Browse variant)* |
| `PerformanceHistoryList` | *(removed — use `EditablePerformanceList` History variant)* |
| `EditablePerformanceList` | Editable performance rows with edit/delete — Browse (My performances) or History (song detail) |
| `TonightDashboard` | Mobile home: tonight context + recent logs |
| `InviteFriendsBanner` | Home prompt when registration is open (invite link or register page) |
| `InviteSharePanel` | Copy registration link/message — full page or compact home banner layout |
| `InviteShareLoader` | Loads invite-share payload + registration info for invite UI |
| `LogSongPickerPanel` | Log page song combobox, working-up shortcut, and inline new-song panel |
| `LogCatalogState` | Mutable catalog snapshot for Log (offline flags, picker items, repertoire/working-up IDs) |
| `MySongsSortToolbar` | Sort dropdown, direction toggle, and genre-filter show/hide for My Songs |
| `MySongsGroupedList` | Genre-grouped repertoire rows with load-more paging |
| `GroupedPagingState` | Load-more paging across genre groups on My Songs |
| `SongDetailFirstLog` | First-performance quick log block on song detail |
| `SongDetailWithHistory` | Co-performers, host message, history, and log-again on song detail |
| `StaleSongsSection` | Home list of songs not performed recently (random sample from your stale pool) |
| `SingerStatsTeaser` | Home summary with link to `/my-stats` |
| `SingerStatsDisplay` | Shared stats layout (used on `/my-stats`) |
| `SingerLinkPanel` | Link login to a singer profile when `SingerId` is missing |
| `MobilePageHeader` | Consistent mobile page title + optional subtitle, back link, or leading content |
| `MobileBackLink` | Standard “Back to More” footer link on account/settings pages |
| `SortDirectionToggle` | Shared newest/oldest sort button for mobile browse pages |
| `NoPerformancesEmptyState` | Preset empty state for “no performances logged yet” with log CTA |
| `OfflineCacheNotice` | Cached-data / offline-unavailable banner for Log and My Songs |
| `RecentLogList` | Recently logged performance rows (links or tap-to-select) |
| `PerformanceEditForm` | Shared date/venue/key/co-performers edit block for history and browse lists |
| `SingerProfileGate` | Resolves singer ID, shows `SingerLinkPanel` when missing, cascades `SingerId` to child content (mobile singer pages + admin `/performances`) |
| `SingerGatedPage` | Mobile page shell: `PageTitle`, `MobilePageHeader`, optional header alerts, and `SingerProfileGate` — used by Log, My Songs, song detail, My performances, My stats |
| `SingerProfileResolver` | Static helper: JWT claim → profile API fallback for singer ID |
| `CatalogSongMapper` | Maps catalog songs to `LogSongPickItem` (used by Log loader and add-to-list panel) |
| `CatalogSongPicker` | Syncfusion song combobox with ★/🎯 badges |
| `MobileSearchToolbar` | Search input + optional result count for mobile browse pages |
| `ChipFilterBar` | Reusable chip filter row (list kind, genre, venue) |
| `LoadMoreButton` | Shared paging footer for mobile browse lists |
| `SongLinkList` | Song title/meta link rows (used by stale-songs section) |
| `PerformanceEditOperations` | Shared update/delete helpers for performance list editors |
| `SingerListResolver` | Find singer list by kind; load lists from API |
| `SingerListActions` | Add/remove song on want-to-sing or working-up lists |
| `AddToWorkingUpButton` | Log page shortcut to add selected song to working up |
| `SongPerformanceSummaryPanel` | Times sung / last performance stats (mobile song detail, admin summary) |
| `SongSummaryHints` | Log page hint text from song performance summary |
| `MobileLoadingState` | Loading wrapper — shows message while loading, otherwise renders child content |
| `StatusAlerts` | Success, error, and warning alerts for mobile pages (supports compact and inline error styles) |
| `MobileEmptyState` | Empty-state message with optional child content and action link |
| `CatalogCrudGrid` | Shared Syncfusion CRUD grid for admin catalog pages (error handling + reload) |
| `SongDisplayMapper` | Maps `SongDto` rows to `SongDisplay` with artist/genre names for the Songs grid |

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
- `GET api/performances/my-stale-songs` — random sample of songs not performed recently (uses saved tickler settings; optional `days` / `limit` / `asOfDate=yyyy-MM-dd`)
- `GET api/performances/my-stats?topVenues=&topSongs=&topArtists=&newRepertoireDays=` — singer totals, recency, ranked lists (0 = omit a section; optional `asOfDate=yyyy-MM-dd`; WASM sends browser-local today)
- `GET api/auth/tickler-settings`, `PUT api/auth/tickler-settings` — per-user stale-song days and limit
- `POST api/performances` — save (singer from JWT if omitted)
- `GET api/auth/me` — singer link status
- `GET api/auth/invite-share` — invite link/message payload for signed-in users (when invite-only registration is configured)

## Auth

- Register/login via API; JWT in browser local storage
- Registration creates a singer and sets `AspNetUsers.SingerId`
- Mobile features need a linked singer (`SingerLinkPanel` if not)

See [security-private-access.md](security-private-access.md) for invite codes and deployment hardening.
