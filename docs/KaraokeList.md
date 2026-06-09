# KaraokeList

> Include this file in requests about the project. For day-to-day singer use, start with [mobile-ux.md](mobile-ux.md).

## Architecture (current)

| Project | Stack | Use |
|---------|-------|-----|
| **KaraokeList.Web** | Blazor WASM + Syncfusion | Primary UI: mobile Log / My Songs + catalog grids |
| **KaraokeList.Api** | ASP.NET Core Web API, JWT, EF Identity | Auth and catalog/performance API |
| **KaraokeList.Shared** | DTOs | Shared between Web and Api |
| **KaraokeList** | Blazor Server (legacy) | Reference; partial parity with WASM |

Catalog and performances live in **Azure SQL / SQL Server**. Legacy SQLite data can be migrated with `scripts/MigrateSqliteToSqlServer`.

Local development: [wasm-api-local-dev.md](wasm-api-local-dev.md).  
Azure deploy: [azure-deployment.md](azure-deployment.md).

## Mobile pages (`KaraokeList.Web`)

| Route | File | Purpose |
|-------|------|---------|
| `/log` | `Pages/Log.razor` | Log performance; optional `?songId=` |
| `/my-songs` | `Pages/MySongs.razor` | Browse repertoire |
| `/my-songs/{id}` | `Pages/MySongDetail.razor` | Log again + history |
| `/more` | `Pages/More.razor` | Catalog hub |
| `/` | `Pages/Home.razor` | Landing |

Details: [mobile-ux.md](mobile-ux.md).

## Catalog pages (`KaraokeList.Web`)

Syncfusion grids (desktop-friendly; linked from **More**):

| Route | File |
|-------|------|
| `/songs` | `Pages/Songs.razor` |
| `/artists` | `Pages/Artists.razor` |
| `/genres` | `Pages/Genres.razor` |
| `/singers` | `Pages/Singers.razor` |
| `/venues` | `Pages/Venues.razor` |
| `/performances` | `Pages/Performances.razor` |

## Performances

Each performance is **one row** per time a singer sang a song at a venue. Aggregates (count, last date) are computed in API queries. See [Performances.md](Performances.md).

## Legacy Blazor Server (`KaraokeList/`)

The Server project still contains catalog grids, Identity account pages, and Syncfusion demo pages under `Components/Pages/`. It is **not** the primary deployment path for the Azure learning branch. Data services under `KaraokeList/Data/` mirror the API SQL access pattern.

## Schema reference

Table-level docs (still accurate for column shapes):

- [Artists.md](Artists.md)
- [Genres.md](Genres.md)
- [Singers.md](Singers.md)
- [Songs.md](Songs.md)
- [Venues.md](Venues.md)
- [Performances.md](Performances.md)

## Auth

- WASM: JWT via `KaraokeList.Api` (`api/auth/login`, `api/auth/register`, `api/auth/me`)
- Friends-only registration: [security-private-access.md](security-private-access.md)
