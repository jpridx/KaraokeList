# Songs

Stores karaoke song metadata.

## Schema

```sql
CREATE TABLE Songs (
    Id              INTEGER       PRIMARY KEY AUTOINCREMENT
                                  UNIQUE
                                  NOT NULL,
    Title           VARCHAR (128) NOT NULL,
    Artist          INTEGER       REFERENCES Artists (Id),
    Genre           INTEGER       REFERENCES Genres (Id),
    Year            INTEGER,
    SecondaryArtist INTEGER
);
```

## Columns

- `Id`: primary key, auto-incrementing integer, unique and required.
- `Title`: song title, required.
- `Artist`: foreign key to `Artists.Id`.
- `Genre`: foreign key to `Genres.Id`.
- `Year`: optional release or publication year.
- `SecondaryArtist`: optional secondary artist identifier.
