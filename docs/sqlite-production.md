# SQLite production (zero Azure SQL cost)

KaraokeList.Api stores catalog, Identity, and performances in a **single SQLite file** instead of Azure SQL. This removes database hosting cost for a personal deployment while keeping the same REST + EF Core stack.

## Connection string

File-based SQLite (default in [KaraokeList.Api/appsettings.json](../KaraokeList.Api/appsettings.json)):

```text
Data Source=/home/data/karaokelist.db
```

Local development ([appsettings.Development.json](../KaraokeList.Api/appsettings.Development.json)):

```text
Data Source=Data/karaokelist.dev.db
```

The API creates the directory for the file on startup (`KaraokeDbPaths.EnsureDataSourceDirectory`) and applies EF migrations plus genre-group seed SQL.

## Azure App Service (Linux API)

1. **Enable persistent storage** on the API App Service:
   - `WEBSITES_ENABLE_APP_SERVICE_STORAGE` = `true`
2. **Upload a populated** `karaokelist.db` to `/home/data/` (Kudu / FTPS) **before** pointing the app at it — see cutover below.
3. Set the connection string:
   - `ConnectionStrings__DefaultConnection` = `Data Source=/home/data/karaokelist.db`
4. Deploy the API (`dotnet publish` / GitHub Actions). Startup runs `MigrateAsync()` (no-op if already migrated) and genre-group seed SQL.

Bicep provisions SQLite-only App Service settings (`infra/main.bicep`). The deploy workflow **does not** flip an existing Azure SQL connection string to SQLite unless you explicitly opt in (`apply_sqlite_connection` on workflow_dispatch) or the app is already on SQLite.

### Scale and availability

- App Service **must run a single instance** (or only one instance may write the file). SQLite is not suitable for multi-instance API scale-out.

### Backups (consistent snapshot)

Do **not** copy only `karaokelist.db` while the API is writing — WAL/journal state can make the copy incomplete or corrupt.

Prefer one of:

1. **Stop the App Service** (or stop writes), then copy `karaokelist.db`, `karaokelist.db-wal`, and `karaokelist.db-shm` together from `/home/data`, then start again.
2. **Online backup** with the SQLite backup API (or `.backup` in the `sqlite3` CLI) against a short-lived connection, writing to a new file under `/home/data`, then download that file.
3. After a quiet period, run `PRAGMA wal_checkpoint(FULL);` (API stopped or single connection) and then copy the main `.db` file.

## Migrating from Azure SQL (#276)

1. Take a final backup of Azure SQL (BACPAC or SSMS).
2. Build a populated local file with [MigrateSqlServerToSqlite](../scripts/MigrateSqlServerToSqlite) — [sqlite-local-verification.md](sqlite-local-verification.md).
3. Enable `WEBSITES_ENABLE_APP_SERVICE_STORAGE` if needed; **upload** the file to `/home/data/karaokelist.db`.
4. Point App Service at SQLite (portal, or **Deploy Azure** → `apply_sqlite_connection=true`) and deploy the SQLite API build.
5. Verify `GET /api/version` (`databaseAvailable`, song counts) and sign-in.
6. Delete the Azure SQL database/server when satisfied (stops SQL billing). Existing SQL resources are **not** removed by Bicep.

## What changed in the API

- EF Core provider: **SQLite** (`UseSqlite`) instead of `UseSqlServer`.
- ADO.NET in [KaraokeList.Api/Data](../KaraokeList.Api/Data) uses `Microsoft.Data.Sqlite` with SQLite-compatible SQL (`LIMIT`, `last_insert_rowid()`, `COALESCE`, `RANDOM()`).
- EF migrations were rebased to a single SQLite baseline (`SqliteInitial`).

Legacy [scripts/MigrateSqliteToSqlServer](../scripts/MigrateSqliteToSqlServer) remains for importing old `.sqlite3` **into SQL Server** if you maintain a SQL Server environment elsewhere.
