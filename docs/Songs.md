# Songs

Stores karaoke song metadata.

## Schema

```sql
CREATE TABLE Songs (
    Id                 INTEGER       PRIMARY KEY AUTOINCREMENT
                                     UNIQUE
                                     NOT NULL,
    Title              VARCHAR (128) NOT NULL,
    Genre              INTEGER       REFERENCES Genres (Id),
    Year               INTEGER,
    RecordingMbid      VARCHAR (36),
    ArtistCreditDisplay VARCHAR (512)
);

CREATE TABLE SongArtists (
    SongId       INTEGER NOT NULL REFERENCES Songs (Id) ON DELETE CASCADE,
    ArtistId     INTEGER NOT NULL REFERENCES Artists (Id) ON DELETE RESTRICT,
    DisplayOrder INTEGER NOT NULL,
    PRIMARY KEY (SongId, ArtistId)
);
```

## Columns

- `Id`: primary key, auto-incrementing integer, unique and required.
- `Title`: song title, required.
- `Genre`: foreign key to `Genres.Id`.
- `Year`: optional release or publication year.
- `RecordingMbid`: optional MusicBrainz recording UUID.
- `ArtistCreditDisplay`: optional formatted artist credit (e.g. from MusicBrainz).

## SongArtists junction

Songs support multiple credited artists via `SongArtists`:

- `DisplayOrder = 0` is the **primary** artist (used for sort and dedup keys).
- Additional artists use `DisplayOrder` 1, 2, …
- Legacy `Songs.Artist` / `Songs.SecondaryArtist` columns were backfilled into `SongArtists` and dropped.
