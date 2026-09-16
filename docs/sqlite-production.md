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

1. **Enable persistent storage** on the API App Service (Configuration → Application settings):
   - `WEBSITES_ENABLE_APP_SERVICE_STORAGE` = `true`
2. Set the connection string (Configuration → Connection strings or environment variables):
   - `ConnectionStrings__DefaultConnection` = `Data Source=/home/data/karaokelist.db`
3. Deploy the API as today (`dotnet publish` / GitHub Actions). On first start, the app creates `/home/data/karaokelist.db` and runs migrations.
4. **Load catalog data** after the empty database exists:
   - Import from a legacy `.sqlite3` with a one-time tool (see below), or
   - Run catalog seed steps adapted for SQLite (export from [scripts/seed-catalog.sql](../scripts/seed-catalog.sql) or use admin import in the app).

### Scale and availability

- App Service **must run a single instance** (or only one instance may write the file). SQLite is not suitable for multi-instance API scale-out.
- Back up by copying `karaokelist.db` from `/home/data` (Kudu / FTPS / scheduled job).

## Migrating from Azure SQL (#276)

You cannot “convert” Azure SQL into the free SQL offer in place; see [issue #276](https://github.com/johnprideaux/KaraokeList/issues/276) for the Azure free-database path if you stay on SQL Server.

If you choose **SQLite instead**:

1. Take a final backup of Azure SQL (BACPAC or SSMS script with schema + data).
2. Deploy this API build with the SQLite connection string on App Service (above).
3. Copy catalog + user data into the new `.db` file (one-time). Options:
   - Re-seed catalog from [scripts/seed-catalog.sql](../scripts/seed-catalog.sql) (translate `USE` / T-SQL to SQLite or use in-app catalog import), then have friends re-register; or
   - Use [scripts/MigrateSqliteToSqlServer](../scripts/MigrateSqliteToSqlServer) in reverse only if you export Azure SQL to a compatible `.sqlite3` first (not automated in-repo yet).
4. Point production traffic at the API, verify `GET /api/version` (`databaseAvailable`, song counts).
5. After verification, delete the Azure SQL database resource to stop billing.

## What changed in the API

- EF Core provider: **SQLite** (`UseSqlite`) instead of `UseSqlServer`.
- ADO.NET in [KaraokeList.Api/Data](../KaraokeList.Api/Data) uses `Microsoft.Data.Sqlite` with SQLite-compatible SQL (`LIMIT`, `last_insert_rowid()`, `COALESCE`, `RANDOM()`).
- EF migrations were rebased to a single SQLite baseline (`SqliteInitial`).

Legacy [scripts/MigrateSqliteToSqlServer](../scripts/MigrateSqliteToSqlServer) remains for importing old `.sqlite3` **into SQL Server** if you maintain a SQL Server environment elsewhere.
