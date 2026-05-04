# Artists

Stores karaoke artist metadata.

## Schema

```sql
CREATE TABLE Artists (
    Id           INTEGER       PRIMARY KEY AUTOINCREMENT
                               UNIQUE
                               NOT NULL,
    Name         VARCHAR (128) NOT NULL,
    SortableName VARCHAR (128),
    MainGenre    INTEGER       REFERENCES Genres (Id)
                               DEFAULT (1)
);
```

## Columns

- `Id`: primary key, auto-incrementing integer, unique and required.
- `Name`: artist display name, required.
- `SortableName`: optional name used for sorting.
- `MainGenre`: foreign key to `Genres.Id`, defaults to `1` when not specified.
