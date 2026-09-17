# Local verification: SQLite + full data migration

Use this checklist before switching production to SQLite. Goal: a local `.db` file with **the same catalog, users, and performances** as your current SQL Server / Azure SQL database, then confirm the WASM + API behave normally.

## Prerequisites

- .NET 10 SDK
- Source database connection string (Azure SQL, LocalDB, or `KARAOKE_SQL_CONNECTION` from your password manager)
- Syncfusion license for WASM (see [wasm-api-local-dev.md](wasm-api-local-dev.md))

## 1. Create an empty SQLite schema

From repo root, pick a target file (Development default shown):

```powershell
$dest = "KaraokeList.Api/Data/karaokelist.dev.db"
Remove-Item $dest -ErrorAction SilentlyContinue
dotnet ef database update --project KaraokeList.Api/KaraokeList.Api.csproj --connection "Data Source=$dest"
```

Linux / cloud agent:

```bash
dest="KaraokeList.Api/Data/karaokelist.dev.db"
rm -f "$dest"
dotnet ef database update --project KaraokeList.Api/KaraokeList.Api.csproj --connection "Data Source=$dest"
```

This applies the `SqliteInitial` migration only (no catalog rows yet).

## 2. Copy all data from SQL Server

Set the **source** connection string, then run the migration tool:

```powershell
$env:KARAOKE_SQL_CONNECTION = "Server=tcp:YOUR-SERVER.database.windows.net,1433;Database=KaraokeList-Dev;Authentication=Active Directory Default;Encrypt=True;"
# Or SQL auth: User ID=...;Password=...;

dotnet run --project scripts/MigrateSqlServerToSqlite/MigrateSqlServerToSqlite.csproj -- $dest
```

Optional: `KARAOKE_SQLITE_PATH` instead of passing the path as an argument.

The tool copies, in order: catalog tables, performances, singer lists, and **ASP.NET Identity** (`AspNetUsers`, roles, tokens). It prints a row-count comparison (`OK` / `MISMATCH`) per table.

**If counts mismatch:** check firewall / VPN to Azure SQL, ensure the source DB is the one you expect, and that EF migrations on SQL Server are up to date before export.

## 3. Point the API at the migrated file

[appsettings.Development.json](../KaraokeList.Api/appsettings.Development.json) already uses:

```text
Data Source=Data/karaokelist.dev.db
```

If you used a different path, update that file or use user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=C:\path\to\karaokelist.dev.db" --project KaraokeList.Api
```

## 4. Run API + WASM locally

```powershell
dotnet run --project KaraokeList.Api/KaraokeList.Api.csproj
dotnet run --project KaraokeList.Web/KaraokeList.Web.csproj
```

Verify:

| Check | How |
|-------|-----|
| Schema + DB file | `GET http://localhost:5299/api/version` → `databaseAvailable: true`, `songCount` matches SQL Server |
| Sign-in | Log in with an **existing** account (password hash was copied) |
| Catalog | Browse Songs / Artists grids |
| Performances | My Songs, Log a performance, stats |

## 5. Production deployment (Azure)

After local verification:

1. **One-time:** Run steps 1–2 on your machine; upload the resulting `.db` to the API App Service (Kudu → `/home/data/karaokelist.db`) *or* run migration on a jump box with network access to Azure SQL, then upload.
2. **App settings** on `api-*` (see [sqlite-production.md](sqlite-production.md)):
   - `WEBSITES_ENABLE_APP_SERVICE_STORAGE` = `true`
   - `ConnectionStrings__DefaultConnection` = `Data Source=/home/data/karaokelist.db`
3. Deploy API + WASM ([github-actions.md](github-actions.md) or manual publish). EF migrations run on **API startup** (no remote SQL step in CD).
4. Smoke: `GET https://api-….azurewebsites.net/api/version` and sign-in on WASM.
5. Delete Azure SQL resources when satisfied (stops SQL billing).

**Infra:** New Bicep deployments default to SQLite (`useAzureSql: false`). Existing stacks can keep SQL resources until you delete them; only the App Service connection string must match SQLite for the app to work.

## 6. Automated tests (optional)

```powershell
dotnet test KaraokeList.Api.IntegrationTests/KaraokeList.Api.IntegrationTests.csproj
```

CI uses a file-based SQLite DB; no LocalDB or Azure SQL required.

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| `songCount: 0` after migration | Wrong source connection string or migration tool run before `dotnet ef database update` on target file |
| Login fails after migration | `AspNetUsers` row count mismatch; re-run tool or check source DB |
| API creates empty DB on Azure | Missing `WEBSITES_ENABLE_APP_SERVICE_STORAGE` or wrong `/home/data/` path |
| Deploy still hits Azure SQL | Update App Service settings; redeploy does not remove old connection string by itself |
