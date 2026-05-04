# Genres

Stores genre lookup values used by artists and songs.

## Schema

```sql
CREATE TABLE Genres (
    Id        INTEGER       PRIMARY KEY AUTOINCREMENT
                            UNIQUE
                            NOT NULL,
    GenreName VARCHAR (128) NOT NULL
);
```

## Columns

- `Id`: primary key, auto-incrementing integer, unique and required.
- `GenreName`: genre label, required.
