# AGENTS.md

## Cursor Cloud specific instructions

### Overview

KaraokeList is a karaoke catalog and performance app on .NET 10:

| Project | Role |
|---------|------|
| **KaraokeList.Web** | Blazor WASM — primary mobile UX (Log, My Songs) + catalog grids |
| **KaraokeList.Api** | JWT auth + SQL catalog/performance API |
| **KaraokeList.Shared** | Shared DTOs |
| **KaraokeList.Web.Tests** | xUnit + bUnit + Moq |
| **KaraokeList.E2E** | Playwright browser tests (WASM + API) |

Data: **Azure SQL / SQL Server** — catalog tables + `Performances` + EF Identity.

### Running locally (primary path)

Two processes:

```bash
dotnet run --project KaraokeList.Api/KaraokeList.Api.csproj
dotnet run --project KaraokeList.Web/KaraokeList.Web.csproj
```

- API: `http://localhost:5299`
- WASM: `http://localhost:5262`

See `docs/wasm-api-local-dev.md` for CORS, JWT, and Syncfusion license setup.

### Mobile routes (`KaraokeList.Web`)

| Route | Purpose |
|-------|---------|
| `/log`, `/log?songId=` | Log performance |
| `/my-songs`, `/my-songs/{id}` | Repertoire browse + detail |
| `/more` | Catalog admin hub |
| `/account/preferences` | Home tickler preferences (days / song limit for **Haven't sung in a while**) |
| `/invite-friends` | Copy invite link/message for friends |
| `/songs`, `/artists`, … | Syncfusion grids |

Details: `docs/mobile-ux.md`. E2E: `docs/e2e-playwright.md`. Admin roles: `docs/admin-roles.md`.

### Key API endpoints

- `POST api/auth/register`, `api/auth/login`, `GET api/auth/me`, `GET api/auth/invite-share`, `GET/PUT api/auth/tickler-settings`
- `GET api/performances/my-repertoire`, `my-repertoire/genres`, `my-song-summary`, `my-stale-songs`, `my-stats`
- `POST api/performances` (auto-fills singer from JWT)

Details: `docs/Performances.md`.

### Database

- Connection string: `ConnectionStrings:DefaultConnection` (LocalDB by default).
- Schema: EF migrations (`dotnet ef database update --project KaraokeList.Api`). Seed: `scripts/MigrateSqliteToSqlServer` or `scripts/seed-catalog.sql`. See `docs/database.md`.
- Azure: `docs/azure-deployment.md`.
- Migrate legacy SQLite: `scripts/MigrateSqliteToSqlServer` with `KARAOKE_SQL_CONNECTION` (pass `.sqlite3` path or use `scripts/data/Karaoke.sqlite3`).

### Key gotchas

- **Work on branches** — never commit or push directly to `master`. Create a feature/fix branch first (e.g. `fix/log-key-default`, `feature/my-performances-nav`), do all work there, and open a PR into `master` when ready.
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
| E2E tests | See `docs/e2e-playwright.md` — `dotnet test KaraokeList.E2E/KaraokeList.E2E.csproj` |
| Run API | `dotnet run --project KaraokeList.Api/KaraokeList.Api.csproj` |
| Run WASM | `dotnet run --project KaraokeList.Web/KaraokeList.Web.csproj` |
| Publish WASM | `dotnet publish KaraokeList.Web/KaraokeList.Web.csproj -c Release` |

### Git workflow

1. Start from updated `master`: `git checkout master && git pull`
2. Create a branch: `git checkout -b fix/short-description` (or `feature/…`, `docs/…`)
3. Commit on that branch only — not on `master`
4. Push the branch and open a PR into `master` (`gh pr create` when available)
5. After merge: `git checkout master && git pull && git branch -d <branch>`

Direct commits to `master` are for merge commits / hotfixes the user explicitly requests on `master`.

### GitHub issues and PRs

When work addresses one or more GitHub issues, **auto-close requires a closing keyword in the PR description** (required for squash merges; commit-message keywords alone are not enough):

```markdown
Closes #48
Closes #49
```

Valid keywords: `Closes`, `Fixes`, `Resolves` (each on its own line, or `Closes #48, closes #49`).

**Do not rely on** `(#48)` or `#48` in the title or commit message — those only link; they do not close issues.

Put closing lines in the **PR body** under `## Summary` or a dedicated `## Issues` section. Mention issue numbers in commits for traceability, but always duplicate `Closes #nn` in the PR description when opening the PR.
