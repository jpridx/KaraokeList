# SingerSongs

Tracks songs performed by singers, including venue and performance dates.

## Schema

```sql
CREATE TABLE SingerSongs (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT
                      UNIQUE
                      NOT NULL,
    Singer    INTEGER REFERENCES Singers (Id),
    Song      INTEGER REFERENCES Songs (Id),
    Venue     INTEGER REFERENCES Venues (Id),
    FirstSung DATE,
    LastSung  DATE,
    Count     INTEGER NOT NULL
                      DEFAULT (0)
);
```

## Columns

- `Id`: primary key, auto-incrementing integer, unique and required.
- `Singer`: foreign key to `Singers.Id`.
- `Song`: foreign key to `Songs.Id`.
- `Venue`: foreign key to `Venues.Id`.
- `FirstSung`: first date the singer performed the song.
- `LastSung`: last date the singer performed the song.
- `Count`: number of times the song was sung, required, defaults to `0`.
