# AGENTS.md

## Cursor Cloud specific instructions

### Overview

KaraokeList is a Blazor Server web application (.NET 10.0) for managing karaoke song catalogs. It uses:

- **Azure SQL / SQL Server** for catalog data (songs, artists, genres, singers, venues, singer_songs) and ASP.NET Identity
- **Parameterized SQL** via `Microsoft.Data.SqlClient` in `KaraokeList/Data/*Service.cs`

### Running the application

```bash
cd KaraokeList
dotnet run --launch-profile http
```

The app listens on `http://localhost:5005`. The `http` profile avoids HTTPS certificate issues in cloud environments.

### Database

- Connection string: `ConnectionStrings:DefaultConnection` in `appsettings.json` (LocalDB by default).
- On startup: EF Identity migrations + `scripts/azure-sql/001-karaoke-schema.sql` for catalog tables.
- Azure deployment: see `docs/azure-deployment.md`.

### SQL Server (local)

Default `appsettings.json` uses LocalDB database `KaraokeList`. Ensure LocalDB is installed, or point `DefaultConnection` at Docker SQL Server:

```
Server=localhost,1433;Database=KaraokeList;User Id=sa;Password=<password>;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### Key gotchas

- **No test projects exist** in this repo. The only verification is `dotnet build`.
- **EF Core tools version conflict**: Do not rely on `dotnet ef database update` in all environments — the app runs `Database.MigrateAsync()` on startup.
- **Syncfusion license key** is optional; use `dotnet user-secrets set "SyncfusionKey" "..." --project KaraokeList.Web` (never commit the key in `wwwroot` or appsettings).
- **HTTPS redirect** can fail in cloud environments; use the `http` launch profile locally.
- **Catalog pages require sign-in** (`[Authorize]` on Songs, Artists, Genres, Singers, Venues, Singer Songs).
- **Migrate legacy SQLite data** with `scripts/MigrateSqliteToSqlServer` and `KARAOKE_SQL_CONNECTION`.

### Build & run commands

| Action | Command |
|--------|---------|
| Restore | `dotnet restore` |
| Build | `dotnet build` |
| Run (dev) | `cd KaraokeList && dotnet run --launch-profile http` |
| Publish | `cd KaraokeList && dotnet publish -c Release` |
