# Singers

Stores singers who perform songs.

## Schema

```sql
CREATE TABLE Singers (
    Id   INTEGER       PRIMARY KEY AUTOINCREMENT
                       UNIQUE
                       NOT NULL,
    Name VARCHAR (128) NOT NULL
);
```

## Columns

- `Id`: primary key, auto-incrementing integer, unique and required.
- `Name`: singer name, required.
