# Deployment roadmap

> Planning doc for hosting KaraokeList in production. Current stack: **KaraokeList.Web** (WASM) + **KaraokeList.Api** (JWT + SQL). See [wasm-api-local-dev.md](wasm-api-local-dev.md).

## Goals

| Target | Purpose |
|--------|---------|
| **Azure** | Primary production host (Static Web Apps + API App Service + SQL) |
| **Winhost.com** | Paused — SSL / nested-subdomain limits; see [winhost-deployment.md](winhost-deployment.md) |
| **Key Vault (Azure)** | Store API secrets; supply Syncfusion key at **build** time in CI |
| **CI/CD** | Repeatable deploys to both hosts without manual zip/FTP mistakes |

## Architecture (production)

```
Browser
   │
   ▼
KaraokeList.Web (static WASM)     ──HTTPS + JWT──▶  KaraokeList.Api (ASP.NET Core)
   │                                                      │
   │                                                      ▼
   └─ Syncfusion key compiled in at publish              SQL Server
      (not a server secret)                              (catalog + Identity + Performances)
```

Two deployables per environment:

1. **API** — `dotnet publish KaraokeList.Api`
2. **WASM** — `dotnet publish KaraokeList.Web` → upload `wwwroot` (and related static output)

Each environment needs its own SQL database, API URL, CORS origins, JWT signing key, and invite code.

---

## Phase 1 — Catalog data migration

Move **non-performance** catalog data into each hosted database before go-live.

### Tables (catalog)

| Table | Contents |
|-------|----------|
| `Genres` | Genre names |
| `Artists` | Artist names |
| `Singers` | Singer roster (linked to `AspNetUsers.SingerId` after friends register) |
| `Venues` | Venue names |
| `Songs` | Song catalog |

### Defer or handle separately

| Data | Notes |
|------|--------|
| `Performances` | Optional for initial load; friends can log at the venue. Migrate later if you have history to preserve. |
| `AspNetUsers` / Identity | Usually **do not** migrate dev accounts. Friends re-register in prod with the invite code. |
| `SingerId` links | Created on registration / `link-singer` in each environment. |

### Source options

- Legacy SQLite: place `Karaoke.sqlite3` at `scripts/data/Karaoke.sqlite3` or pass the path to `scripts/MigrateSqliteToSqlServer`; set `KARAOKE_SQL_CONNECTION`.
- LocalDB / dev SQL Server (export or point migration tool at source)

### Migration tool

```powershell
$env:KARAOKE_SQL_CONNECTION = "Server=...;Database=KaraokeList;..."
dotnet run --project scripts/MigrateSqliteToSqlServer/MigrateSqliteToSqlServer.csproj
```

Run **once per target database** (Winhost SQL, Azure SQL). The tool currently migrates all tables including `Performances`; for catalog-only, either use an empty/local DB without performance rows or extend the tool to skip tables (future task).

### After migration

1. Ensure schema exists: `dotnet ef database update --project KaraokeList.Api` (or API startup `MigrateAsync()`). See [database.md](database.md).
2. Smoke-test: sign in, browse Songs grid, open My Songs (all catalog).
3. Repeat for the second host if both are live.

**Checklist**

- [ ] Catalog migrated to **Azure SQL**
- [ ] Catalog migrated to **Winhost SQL**
- [ ] Performances strategy decided (empty vs migrate)
- [ ] Production invite code generated ([security-private-access.md](security-private-access.md))

---

## Phase 2 — Azure

Bicep: `infra/main.bicep` — **API App Service** + **Static Web Apps** (WASM) + Azure SQL. Deploy guide: [azure-deployment.md](azure-deployment.md). Helper: `scripts/deploy-azure.ps1`. CI/CD: [github-actions.md](github-actions.md).

### 2a — Key Vault

Store **server-side** secrets only:

| Secret name (suggested) | Maps to API setting |
|-------------------------|---------------------|
| `SqlConnectionString` | `ConnectionStrings__DefaultConnection` |
| `JwtSigningKey` | `Jwt__Key` (32+ chars) |
| `RegistrationInviteCode` | `Security__Registration__InviteCode` |

Optional pipeline-only secret:

| Secret | Use |
|--------|-----|
| `SyncfusionKey` | `dotnet publish ... /p:SyncfusionKey=...` for WASM — **not** runtime on App Service |

Wire API App Service **managed identity** → Key Vault (RBAC: *Key Vault Secrets User*). App settings use references:

```text
@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/JwtSigningKey/)
```

**Checklist**

- [ ] Create Key Vault (Bicep or portal)
- [ ] Store secrets; remove plain SQL password from Bicep app settings where possible
- [ ] API managed identity + Key Vault access policy / RBAC
- [ ] Key Vault references on API app settings
- [ ] Document vault name and secret names in password manager (not in git)

### 2b — Azure resources (target layout)

| Resource | Hosts |
|----------|--------|
| App Service (or Static Web Apps) | `KaraokeList.Web` static output |
| App Service (Linux, .NET 10) | `KaraokeList.Api` |
| Azure SQL | Database |
| Key Vault | Secrets |

API app settings (production):

| Setting | Value |
|---------|--------|
| `Jwt__Issuer` / `Jwt__Audience` | `KaraokeList` / `KaraokeList.Web` |
| `Cors__Origins__0` | `https://<wasm-host>` |
| `Security__Registration__RequireInviteCode` | `true` |
| `Security__Registration__AllowRegistration` | `true` until group joined |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

Follow [azure-deployment.md](azure-deployment.md) for first deploy (provision → secrets → publish).

### 2c — CI/CD pipeline (Azure)

Suggested: GitHub Actions workflows `.github/workflows/ci.yml` and `.github/workflows/deploy-azure.yml`. Setup: [github-actions.md](github-actions.md).

**API job**

1. `dotnet publish KaraokeList.Api -c Release`
2. Deploy zip to API App Service (`az webapp deployment` or GitHub Action)

**WASM job**

1. Read `SyncfusionKey` from Key Vault or GitHub secret
2. `dotnet publish KaraokeList.Web -c Release /p:SyncfusionKey=...`
3. Set WASM `appsettings` / `wwwroot/appsettings.json` production API base URL (or build-time substitution)
4. Deploy static output to Static Web Apps or static App Service

**Checklist**

- [ ] GitHub/Azure OIDC — see [github-actions.md](github-actions.md)
- [ ] Pipeline secrets (Syncfusion, Azure IDs)
- [x] Azure workflow: build + deploy API + WASM (`.github/workflows/deploy-azure.yml`)
- [ ] Post-deploy smoke test (login, My Songs, Log)

---

## Phase 3 — Winhost.com

**Detailed guide:** [winhost-deployment.md](winhost-deployment.md) — one database, Cloudflare + origin cert HTTPS, pre-flight checklist, production config.

Winhost has no native Azure Key Vault integration. Equivalent:

| Secret | Where |
|--------|--------|
| SQL connection string | Winhost SQL + API app settings / `web.config` / panel |
| JWT key, invite code | Winhost control panel application settings for API site |
| Syncfusion key | **CI secret only** → `/p:SyncfusionKey` at publish |

### 3a — Winhost layout (target)

| Site | Contents |
|------|----------|
| API subdomain (e.g. `api.example.com`) | `KaraokeList.Api` (.NET 10 if available; confirm host runtime) |
| Main domain or `app.` subdomain | WASM static files from `publish/wwwroot` |
| Winhost SQL Server | Same schema as Azure |

Confirm with Winhost: supported .NET version, Web Deploy vs FTP, HTTPS, custom domains, SQL firewall from API.

### 3b — CI/CD pipeline (Winhost)

Suggested: `deploy-winhost.yml` — separate from Azure.

1. Build & publish API → Web Deploy / FTP to API site
2. Build WASM with `SYNCFUSION_KEY` from GitHub secret → deploy static site
3. Optional: run migration script in pipeline (careful — destructive `DELETE` in migration tool)

**Checklist**

- [ ] Winhost sites + SQL provisioned
- [ ] Catalog migrated (Phase 1)
- [ ] API app settings in panel (connection string, JWT, invite, CORS)
- [ ] WASM points at Winhost API URL
- [ ] GitHub secrets for deploy credentials + Syncfusion
- [ ] Winhost workflow tested end-to-end

---

## Phase 4 — Ongoing operations

### Two live databases

If both Winhost and Azure stay up:

- Pick a **source of truth** for catalog edits, or
- Accept manual sync (re-run migration when you bulk-add songs — migration tool **deletes** target rows first)

### Secret rotation

| Secret | Rotate how |
|--------|------------|
| Invite code | App setting / Key Vault; existing users unaffected |
| JWT key | Key Vault + API restart; **invalidates all tokens** |
| SQL password | DB + connection string + restart API |

### Syncfusion (reminder)

- WASM key is **compiled in** at publish — not hidden from browsers.
- User secrets = fine for solo local publish.
- Key Vault / pipeline secrets = hygiene for CI and teams, not client-side protection.

---

## Master checklist

### Data

- [ ] Catalog on Azure SQL
- [ ] Catalog on Winhost SQL
- [ ] Performances / Identity approach documented per environment

### Azure

- [ ] Key Vault + API references
- [ ] WASM + API hosted
- [ ] CI/CD pipeline — [github-actions.md](github-actions.md)
- [x] Refresh [azure-deployment.md](azure-deployment.md)

### Winhost

- [ ] WASM + API hosted
- [ ] CI/CD pipeline
- [ ] Follow [winhost-deployment.md](winhost-deployment.md) (DB, Cloudflare, secrets)

### Security (both)

- [ ] Production JWT key (≠ dev key in `appsettings.json`)
- [ ] Invite code set; registration closed when group is complete
- [ ] CORS limited to production WASM origin(s)
- [ ] HTTPS only

---

## Related docs

| Doc | Topic |
|-----|--------|
| [wasm-api-local-dev.md](wasm-api-local-dev.md) | Local two-process dev |
| [winhost-deployment.md](winhost-deployment.md) | Winhost, one DB, Cloudflare HTTPS |
| [azure-deployment.md](azure-deployment.md) | Azure WASM + API deploy |
| [security-private-access.md](security-private-access.md) | Invite code, registration |
| [mobile-ux.md](mobile-ux.md) | Singer-facing flows |
| [Performances.md](Performances.md) | Performance schema / API |
