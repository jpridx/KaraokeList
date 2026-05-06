# AGENTS.md

## Cursor Cloud specific instructions

### Overview

KaraokeList is a Blazor Server web application (.NET 10.0) for managing karaoke song catalogs. It uses:
- **SQLite** for business data (songs, artists, genres, singers, venues, singer_songs) — embedded file at `KaraokeList/Temp/Karaoke.sqlite3`
- **SQL Server** for ASP.NET Identity (authentication) — runs in a Docker container

### Running the application

```bash
cd /workspace/KaraokeList
dotnet run --launch-profile http
```

The app listens on `http://localhost:5005`. The `http` profile avoids HTTPS certificate issues in cloud environments.

### SQL Server (Identity Database)

The Identity database requires a SQL Server instance. Start it via Docker:

```bash
dockerd &>/var/log/dockerd.log &
sleep 3
docker start sqlserver 2>/dev/null || \
  docker run -d --name sqlserver \
    -e 'ACCEPT_EULA=Y' \
    -e 'SA_PASSWORD=KaraokeP@ss123' \
    -e 'MSSQL_PID=Express' \
    -p 1433:1433 \
    mcr.microsoft.com/mssql/server:2022-latest
```

The connection string is configured via .NET user secrets:
```
Server=localhost,1433;Database=KaraokeListIdentity;User Id=sa;Password=KaraokeP@ss123;TrustServerCertificate=True;MultipleActiveResultSets=true
```

If the Identity database needs to be recreated from scratch, apply the schema via:
```bash
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'KaraokeP@ss123' -C -Q "CREATE DATABASE KaraokeListIdentity"
```
Then apply the ASP.NET Identity tables from the migration in `KaraokeList/Data/Migrations/00000000000000_CreateIdentitySchema.cs`.

### Key gotchas

- **No test projects exist** in this repo. The only verification is `dotnet build`.
- **No explicit linting/analyzers** configured. `dotnet build` with 0 warnings is the lint check.
- **EF Core tools version conflict**: The global `dotnet-ef` tool (10.0.x) conflicts with the `Microsoft.EntityFrameworkCore.Design` assembly resolution. Do not rely on `dotnet ef database update` — apply schema changes via raw SQL using `sqlcmd` inside the Docker container.
- **Syncfusion license key** is optional. The app runs without it (shows a trial watermark but is fully functional).
- **HTTPS redirect** will fail in cloud environments; always use the `http` launch profile.
- **The SQLite database file** is bundled in the repo and copied to the output directory on build. No migration needed for business data.

### Build & run commands

| Action | Command |
|--------|---------|
| Restore | `dotnet restore` |
| Build | `dotnet build` |
| Run (dev) | `cd KaraokeList && dotnet run --launch-profile http` |
