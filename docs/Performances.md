# Performances

Each row is **one time** a singer performed a song at a venue — a performance log, not a summary.

## Schema

```sql
CREATE TABLE Performances (
    Id                 INT IDENTITY PRIMARY KEY,
    Singer             INT NULL REFERENCES Singers (Id),
    Song               INT NULL REFERENCES Songs (Id),
    Venue              INT NULL REFERENCES Venues (Id),
    PerformedOn        DATE NOT NULL,
    KeyChangeSemitones INT NULL
);
```

## Columns

- `PerformedOn`: date of that performance.
- `KeyChangeSemitones`: optional half-step change from the song's original key (`+2` = up two half-steps, `-1` = down one). `NULL` or `0` means original key.

## Questions this supports

| Question | How |
|----------|-----|
| How many times have I sung this song? | `COUNT(*)` where `Singer` + `Song` match |
| Where/when have I sung it? | Rows ordered by `PerformedOn`, join `Venues` |
| What key did I use last time? | Latest row by `PerformedOn` for that singer + song |

## API (authenticated)

| Endpoint | Purpose |
|----------|---------|
| `GET api/performances/my-repertoire?sortBy=&sortDir=&genreId=&includeAll=` | Repertoire list. Default: songs you've performed. `includeAll=true`: entire catalog with `PerformanceCount` / `LastPerformedOn` (null when not logged). `sortBy`: `title`, `artist`, `genre`, `lastPerformed`. `sortDir`: `asc`, `desc`. |
| `GET api/performances/my-repertoire/genres` | Distinct genres in your repertoire (for filter chips). |
| `GET api/performances/my-history?venueId=&sortDir=` | Enriched performance rows for mobile browse (song, artist, venue names). |
| `GET api/performances/my-song-summary?songId=` | Count, last key/venue/date, full history for one song. |
| `PUT api/singers/me/songs/{songId}/genre` | Update a song's catalog genre (member; linked singer required). |
| `POST api/performances` | Log a performance; `Singer` defaults from the logged-in user when omitted. |

On **Log** (`QuickLogPerformance`), the key picker defaults to `LastKeyChangeSemitones` from `my-song-summary` when you've performed that song before; otherwise it defaults to original key (`NULL` / 0). It does **not** reuse the key from your previous log entry for a different song. Venue defaults from your last log on this device; the date defaults to **today** (change it for backfill — **Use today** resets the picker).

## Mobile UI (Blazor WASM)

Full walkthrough: [mobile-ux.md](mobile-ux.md).

| Route | Purpose |
|-------|---------|
| `/log` | Pick song (ComboBox), `QuickLogPerformance` form, + new song/venue |
| `/log?songId=` | Log with song pre-selected |
| `/my-songs` | Repertoire browse: search, sort, genre group filter (optional detailed genres), group-by-genre, per-row **Log** |
| `/my-songs/{id}` | Stats + copy for host + change genre + collapsible log/history |
| `/my-performances` | All your performances: search, venue filter, edit/delete |
| `/more` | Catalog admin hub |

**Copy for host** — `Title - Artist` or `Title - Artist (Down N)` / `(Up N)` via `ShowHostMessageFormatting`.
