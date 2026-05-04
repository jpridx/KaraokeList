# Venues

Stores performance venues referenced by singer-song records.

## Schema

```sql
CREATE TABLE Venues (
    Id        INTEGER       PRIMARY KEY AUTOINCREMENT
                            UNIQUE
                            NOT NULL,
    VenueName VARCHAR (128) NOT NULL
);
```

## Columns

- `Id`: primary key, auto-incrementing integer, unique and required.
- `VenueName`: venue name, required.
