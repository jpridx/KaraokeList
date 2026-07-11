# Genres

Stores genre lookup values used by artists and songs, plus fixed broad **genre groups** for karaoke browsing (#62).

## Schema

### Genres

```sql
CREATE TABLE Genres (
    Id        INT           PRIMARY KEY IDENTITY,
    GenreName NVARCHAR(MAX) NOT NULL
);
```

### Genre groups (fixed taxonomy)

Six seeded groups classify leaf genres for My Songs two-level grouping:

| Sort | GroupName |
|------|-----------|
| 1 | Rock |
| 2 | Pop |
| 3 | Country |
| 4 | Christian |
| 5 | R&B / Soul |
| 6 | Standards & Show Tunes |

```sql
CREATE TABLE GenreGroups (
    Id        INT            PRIMARY KEY IDENTITY,
    GroupName NVARCHAR(128)  NOT NULL UNIQUE,
    SortOrder INT            NOT NULL
);

CREATE TABLE GenreGroupGenres (
    GenreGroupId INT NOT NULL,
    GenreId      INT NOT NULL,
    IsPrimary    BIT NOT NULL,
    PRIMARY KEY (GenreGroupId, GenreId),
    FOREIGN KEY (GenreGroupId) REFERENCES GenreGroups(Id) ON DELETE CASCADE,
    FOREIGN KEY (GenreId) REFERENCES Genres(Id) ON DELETE CASCADE
);
```

- A genre may belong to **more than one** group (many-to-many).
- Exactly one membership per genre should be marked `IsPrimary = 1` when multi-assigned; My Songs grouped view uses the primary group.
- Unmapped genres (e.g. Comedy, Unclassified) appear under **Other** in the grouped list.

## Seeding / classifying genres

- **New databases:** EF migration `AddGenreGroups` seeds groups and maps genres by `GenreName`.
- **Existing databases:** run [`scripts/seed-genre-groups.sql`](../scripts/seed-genre-groups.sql) (idempotent).

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KaraokeList -i scripts/seed-genre-groups.sql
```

## API

| Method | Route | Auth |
|--------|-------|------|
| GET | `api/genre-groups` | Signed-in |
| PUT | `api/genre-groups/{id}/genres` | Admin |

## Admin UI

- `/genre-groups` — assign genres to each fixed group; set primary group for multi-group genres.
- Linked from **More → Catalog**.

## Columns (Genres)

- `Id`: primary key, auto-incrementing integer.
- `GenreName`: genre label, required.
