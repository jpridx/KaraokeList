# AGENTS.md

## Cursor Cloud specific instructions

### Overview

KaraokeList is a karaoke catalog and performance app on .NET 10:

| Project | Role |
|---------|------|
| **KaraokeList.Web** | Blazor WASM — primary mobile UX (Log, My Songs) + catalog grids |
| **KaraokeList.Api** | JWT auth + SQL catalog/performance API |
| **KaraokeList.Shared** | Shared DTOs |
| **KaraokeList** | Legacy Blazor Server (reference) |

Data: **Azure SQL / SQL Server** — catalog tables + `Performances` + EF Identity. Legacy `SingerSongs` was migrated to `Performances`.

### Running locally (primary path)

Two processes:

```bash
dotnet run --project KaraokeList.Api/KaraokeList.Api.csproj
dotnet run --project KaraokeList.Web/KaraokeList.Web.csproj
```

- API: `http://localhost:5299`
- WASM: `http://localhost:5262`

See `docs/wasm-api-local-dev.md` for CORS, JWT, and Syncfusion license setup.

### Legacy Blazor Server

```bash
cd KaraokeList
dotnet run --launch-profile http
```

Listens on `http://localhost:5005`. Use the `http` profile to avoid HTTPS certificate issues in cloud environments.

### Mobile routes (`KaraokeList.Web`)

| Route | Purpose |
|-------|---------|
| `/log`, `/log?songId=` | Log performance |
| `/my-songs`, `/my-songs/{id}` | Repertoire browse + detail |
| `/more` | Catalog admin hub |
| `/songs`, `/artists`, … | Syncfusion grids |

Details: `docs/mobile-ux.md`.

### Key API endpoints

- `POST api/auth/register`, `api/auth/login`, `GET api/auth/me`
- `GET api/performances/my-repertoire`, `my-repertoire/genres`, `my-song-summary`
- `POST api/performances` (auto-fills singer from JWT)

Details: `docs/Performances.md`.

### Database

- Connection string: `ConnectionStrings:DefaultConnection` (LocalDB by default).
- API startup: EF Identity migrations + `scripts/azure-sql/001-karaoke-schema.sql`.
- Azure: `docs/azure-deployment.md`.
- Migrate legacy SQLite: `scripts/MigrateSqliteToSqlServer` with `KARAOKE_SQL_CONNECTION`.

### Key gotchas

- **No test projects** — verification is `dotnet build`.
- **Run both Api and Web** for the singer mobile flows; WASM calls the API.
- **Syncfusion license** — `dotnet user-secrets set "SyncfusionKey" "..." --project KaraokeList.Web`, then build (generates gitignored `SyncfusionLicenseKey.g.cs`). Or `scripts/set-syncfusion-key.ps1`. Never commit keys.
- **Catalog pages require sign-in** (`[Authorize]` on data pages).
- **HTTPS redirect** can fail in cloud environments; use `http` launch profiles locally.

### Build & run commands

| Action | Command |
|--------|---------|
| Restore | `dotnet restore` |
| Build | `dotnet build` |
| Run API | `dotnet run --project KaraokeList.Api/KaraokeList.Api.csproj` |
| Run WASM | `dotnet run --project KaraokeList.Web/KaraokeList.Web.csproj` |
| Run Server (legacy) | `cd KaraokeList && dotnet run --launch-profile http` |
| Publish WASM | `dotnet publish KaraokeList.Web/KaraokeList.Web.csproj -c Release` |
