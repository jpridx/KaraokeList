# Database schema and seed data

KaraokeList uses **EF Core migrations** for all tables (ASP.NET Identity + catalog). Seed data is a **separate, explicit step** — not applied on API startup.

## Schema (EF migrations)

Migrations live in `KaraokeList.Api/Data/Migrations/`:

| Migration | Creates |
|-----------|---------|
| `20260608030430_InitialCreate` | Identity tables + `Genres`, `Artists`, `Singers`, `Venues`, `Songs`, `Performances` |
| `20260608031358_AddDbSetsAndRelations` | `AspNetUsers.SingerId` → `Singers` foreign key |
| `20260616033911_UniqueArtistName` | Unique index on `Artists.Name` |
| `20260623235540_AddCatalogForeignKeys` | Performance/song/singer/venue FKs; song→artist FKs; `Performances.Song`/`Singer` NOT NULL |
| `20260711012300_AddGenreGroups` | `GenreGroups`, `GenreGroupGenres`; seeds six fixed groups + genre mappings |

### Apply schema

From repo root (uses connection string from API config / user secrets):

```powershell
dotnet ef database update --project KaraokeList.Api
```

Or start the API once — `Program.cs` calls `MigrateAsync()` on startup (schema only, no seed).

`dotnet ef database update` and API startup both apply pending migrations. You do **not** need to deploy or run SQL DDL scripts for catalog tables.

### Add a new schema change

```powershell
dotnet ef migrations add YourMigrationName --project KaraokeList.Api
dotnet ef database update --project KaraokeList.Api
```

## Seed data (explicit)

The API does **not** insert catalog rows at startup. Choose one:

### Option 1 — SQLite / existing SQL Server → target database

```powershell
$env:KARAOKE_SQL_CONNECTION = "Server=...;Database=KaraokeList-Dev;..."
dotnet run --project scripts/MigrateSqliteToSqlServer/MigrateSqliteToSqlServer.csproj -- scripts/data/Karaoke.sqlite3
```

Pass your `.sqlite3` path as the first argument, or place the file at `scripts/data/Karaoke.sqlite3` before running without arguments.

Migrates `Genres`, `Artists`, `Singers`, `Venues`, `Songs`, and optionally `Performances`. See [deployment-roadmap.md](deployment-roadmap.md) Phase 1.

### Option 2 — Catalog seed SQL (primary)

The repo includes your catalog seed at [scripts/seed-catalog.sql](../scripts/seed-catalog.sql) (`Genres`, `Artists`, `Songs`, `Venues` with fixed IDs).

1. Apply schema first: `dotnet ef database update --project KaraokeList.Api`
2. Edit the `USE […]` line at the top of `seed-catalog.sql` if your database name differs (default: `KaraokeList-Dev`).
3. Run on **empty** catalog tables (first load). To re-seed, delete catalog rows first (order: `Performances`, `Songs`, `Artists`, `Venues`, `Genres` — skip tables you did not seed).

**SSMS / Azure Data Studio:** open `scripts/seed-catalog.sql`, connect to your server, execute.

**sqlcmd (PowerShell helper):**

```powershell
.\scripts\Invoke-SeedCatalog.ps1 -Server "karaokelist.database.windows.net" -Database "KaraokeList-Dev" -UseAzureActiveDirectory
```

Or with a full connection string:

```powershell
$env:KARAOKE_SQL_CONNECTION = "Server=tcp:....database.windows.net,1433;Database=KaraokeList-Dev;Authentication=Active Directory Default;Encrypt=True;"
.\scripts\Invoke-SeedCatalog.ps1
```

### Genre group classification

After catalog genres exist, classify them into broad karaoke groups (Rock, Pop, Country, etc.):

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KaraokeList -i scripts/seed-genre-groups.sql
```

Fresh installs that run `dotnet ef database update` get groups and mappings from migration `AddGenreGroups`. Re-run the script after adding new leaf genres. See [Genres.md](Genres.md).

### Option 3 — EF seed migration (future)

For repeatable dev fixtures, add a migration with `migrationBuilder.InsertData(...)` or SQL in `Up()`. Keep seed migrations separate from schema migrations.

## Legacy SQL scripts

Files under `scripts/azure-sql/` are **reference only** and are **not** executed by the API. Schema source of truth is EF migrations.

## Test database alignment

If `__EFMigrationsHistory__` already contains:

- `20260608030430_InitialCreate`
- `20260608031358_AddDbSetsAndRelations`

…you are aligned with the repo. No history rewrite needed.

If it contains older identity-only migration IDs (`00000000000000_CreateIdentitySchema`, etc.), replace them with the two rows above or run `dotnet ef database update` on a fresh database.

## Data integrity & account lifecycle

Referential integrity analysis, delete policy, anonymization plan, and phased rollout: **[data-integrity.md](data-integrity.md)**.

Summary backlog:

| Item | Notes |
|------|--------|
| **Account anonymization (quit app)** | Anonymize PII and revoke login; retain `Singers` + `Performances`. |
| **Catalog foreign keys** | EF migration with explicit `ON DELETE` rules; block admin deletes when referenced. |
| **Stop performance hard-delete** | Soft-delete or disallow once account policy is settled. |
