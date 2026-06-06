# KaraokeList

A karaoke catalog and performance tracker: mobile-first logging at the venue, plus Syncfusion grids for catalog admin.

## Overview

**KaraokeList.Web** (Blazor WebAssembly) is the primary UI for singers — log performances, browse songs you've sung, and copy a formatted message for the KJ. **KaraokeList.Api** provides JWT auth and a SQL-backed catalog/performance API. **KaraokeList** (Blazor Server) remains as a legacy reference app.

## Technology Stack

| Layer | Stack |
|-------|-------|
| UI (primary) | Blazor WASM, Syncfusion Blazor (Fluent 2), Bootstrap |
| API | ASP.NET Core Web API, JWT, EF Core Identity |
| Database | Azure SQL / SQL Server |
| Shared | `KaraokeList.Shared` DTOs |

## Features

### Mobile (singers)

- **Log** — pick a song, set venue/date/key, copy for host, save
- **My Songs** — repertoire browse with search, sort, genre filter
- **Song detail** — log again + performance history
- **Copy for host** — `Title - Artist` with `(Up N)` / `(Down N)` when key differs

See [docs/mobile-ux.md](docs/mobile-ux.md).

### Catalog admin (grids)

- Songs, Artists, Genres, Singers, Venues, Performances
- Full CRUD via Syncfusion grids (paging, sort, filter, inline edit)
- Reachable from **More → Catalog** on mobile layout

### Auth

- Invite-code registration, sign-in required for data pages
- JWT in WASM; singer linked to login for performance logging

See [docs/security-private-access.md](docs/security-private-access.md).

## Project Structure

```
KaraokeList/
├── KaraokeList.Web/          # Blazor WASM (primary UI)
├── KaraokeList.Api/          # Web API + Identity
├── KaraokeList.Shared/       # DTOs
├── KaraokeList/              # Legacy Blazor Server
├── scripts/                  # SQL schema, migrations, Syncfusion key helper
└── docs/                     # Documentation
```

## Getting Started

### Prerequisites

- .NET 9.0 or .NET 10.0 SDK
- SQL Server LocalDB (or Docker SQL Server)

### Local development (WASM + API)

1. Clone the repository
2. Restore: `dotnet restore`
3. Run API and WASM (two terminals):

```powershell
dotnet run --project KaraokeList.Api/KaraokeList.Api.csproj
dotnet run --project KaraokeList.Web/KaraokeList.Web.csproj
```

- API: `http://localhost:5299`
- WASM: `http://localhost:5262`

Full setup (Syncfusion license, auth, CORS): [docs/wasm-api-local-dev.md](docs/wasm-api-local-dev.md).

### Legacy Blazor Server

```powershell
cd KaraokeList
dotnet run --launch-profile http
```

Listens on `http://localhost:5005`.

## Database

Catalog and Identity share one SQL Server database via `ConnectionStrings:DefaultConnection`.

**Local default (LocalDB):**

```
Server=(localdb)\mssqllocaldb;Database=KaraokeList;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Catalog tables are applied from `scripts/azure-sql/001-karaoke-schema.sql` on API startup. Performances replace the legacy `SingerSongs` model — see [docs/Performances.md](docs/Performances.md).

**Azure:** [docs/azure-deployment.md](docs/azure-deployment.md)

### Tables

- **Songs**, **Artists**, **Genres**, **Singers**, **Venues** — catalog
- **Performances** — one row per time a singer performed a song at a venue
- **AspNetUsers** / Identity — auth; `SingerId` links login to singer

Schema details: `docs/*.md`.

## Documentation

| Doc | Contents |
|-----|----------|
| [docs/mobile-ux.md](docs/mobile-ux.md) | Log, My Songs, copy-for-host, navigation |
| [docs/KaraokeList.md](docs/KaraokeList.md) | Architecture and page map |
| [docs/Performances.md](docs/Performances.md) | Performance schema and API |
| [docs/wasm-api-local-dev.md](docs/wasm-api-local-dev.md) | Run WASM + API locally |
| [docs/azure-deployment.md](docs/azure-deployment.md) | Azure App Service deploy |
| [docs/deployment-roadmap.md](docs/deployment-roadmap.md) | Winhost + Azure + Key Vault + CI/CD plan |
| [docs/winhost-deployment.md](docs/winhost-deployment.md) | Winhost hosting, one DB, Cloudflare HTTPS |
| [docs/security-private-access.md](docs/security-private-access.md) | Invite codes and hardening |

## License

[Add your license information here]
