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

Legacy `SingerSongs` (FirstSung / LastSung / Count) is migrated by `scripts/azure-sql/002-migrate-singer-songs-to-performances.sql`.

## API (authenticated)

| Endpoint | Purpose |
|----------|---------|
| `GET api/performances/my-repertoire?sortBy=&sortDir=&genreId=` | Songs you've performed (aggregated). `sortBy`: `title`, `artist`, `genre`, `lastPerformed`. `sortDir`: `asc`, `desc`. |
| `GET api/performances/my-repertoire/genres` | Distinct genres in your repertoire (for filter chips). |
| `GET api/performances/my-song-summary?songId=` | Count, last key/venue/date, full history for one song. |
| `POST api/performances` | Log a performance; `Singer` defaults from the logged-in user when omitted. |

## Mobile UI (Blazor WASM)

- `/log` — quick log performance, inline new song/venue
- `/my-songs` — browse repertoire with sort, genre filter, optional group-by-genre
- `/my-songs/{id}` — performance history for one song
