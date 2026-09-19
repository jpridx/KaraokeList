# Database schema and seed data

KaraokeList uses **SQLite** (single file) with **EF Core migrations** for all tables (ASP.NET Identity + catalog). Local cutover and production: [sqlite-local-verification.md](sqlite-local-verification.md), [sqlite-production.md](sqlite-production.md).

## Schema (EF migrations)

Migrations live in `KaraokeList.Api/Data/Migrations/`. The SQL Server migration chain was replaced by a single SQLite baseline:

| Migration | Creates |
|-----------|---------|
| `20260915235636_SqliteInitial` | Identity + catalog + performances + singer lists + genre groups + `SongArtists` (full current schema) |

### Apply schema

From repo root (design-time resolves `Data/…` under `KaraokeList.Api/` regardless of cwd):

```powershell
dotnet ef database update --project KaraokeList.Api
```

Or start the API once — `Program.cs` calls `MigrateAsync()` on startup, then seeds **genre groups / mappings** (not catalog songs/artists).

### Add a new schema change

```powershell
dotnet ef migrations add YourMigrationName --project KaraokeList.Api
dotnet ef database update --project KaraokeList.Api
```

## Seed data

| What | When |
|------|------|
| Genre groups + genre↔group mappings | Automatic after migrations (`Program.cs` + `GenreGroupSeedSql`) |
| Catalog (genres, artists, songs, venues) and users/performances | Explicit — copy from SQL Server or run a seed script |

### Preferred — copy from Azure SQL / SQL Server

```powershell
# 1. Empty schema
New-Item -ItemType Directory -Force -Path "KaraokeList.Api\Data" | Out-Null
$dest = "KaraokeList.Api/Data/karaokelist.dev.db"
dotnet ef database update --project KaraokeList.Api/KaraokeList.Api.csproj --connection "Data Source=$dest"

# 2. Full data + Identity
$env:KARAOKE_SQL_CONNECTION = "Server=tcp:YOUR-SERVER.database.windows.net,1433;Database=KaraokeList;Authentication=Active Directory Default;Encrypt=True;"
dotnet run --project scripts/MigrateSqlServerToSqlite/MigrateSqlServerToSqlite.csproj -- $dest
```

Details: [sqlite-local-verification.md](sqlite-local-verification.md).

### Legacy — catalog SQL / old `.sqlite3` → SQL Server

[scripts/seed-catalog.sql](../scripts/seed-catalog.sql) and [scripts/MigrateSqliteToSqlServer](../scripts/MigrateSqliteToSqlServer) target **SQL Server**. Prefer `MigrateSqlServerToSqlite` when the API is on SQLite.

## Test database alignment

Fresh SQLite files should contain only:

- `20260915235636_SqliteInitial`

in `__EFMigrationsHistory`. Older SQL Server migration IDs are obsolete; recreate the file rather than rewriting history.

## Data integrity & account lifecycle

Referential integrity analysis, delete policy, anonymization plan, and phased rollout: **[data-integrity.md](data-integrity.md)**.
