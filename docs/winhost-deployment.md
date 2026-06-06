# Winhost deployment

> Hosting **KaraokeList.Web** (WASM) + **KaraokeList.Api** on [Winhost](https://www.winhost.com/) with **one MS SQL database**, **HTTPS via Cloudflare**, and (planned) GitHub Actions CI/CD. See [deployment-roadmap.md](deployment-roadmap.md) for the full multi-environment plan.

## Architecture

```text
Browser
   │  HTTPS (public)
   ▼
Cloudflare proxy (edge TLS)
   │  HTTPS with origin cert (recommended: Full / Full strict)
   ▼
Winhost IIS
   ├── Main site ────────── KaraokeList.Web (static WASM: index.html, _framework/, …)
   └── api. subdomain ───── KaraokeList.Api (ASP.NET Core 10, framework-dependent)
              │
              ▼
         One SQL Server database (e.g. KaraokeList)
              ├── EF Identity (AspNetUsers, …)
              └── Catalog + Performances (dbo.*)
```

| Public URL (use in app config) | Hosts |
|--------------------------------|--------|
| `https://<your-wasm-host>/` | Blazor WASM |
| `https://api.<your-domain>/` | Web API |

Deploy uploads go to the **Winhost origin** (panel hostname / origin IP), **not** through Cloudflare. Only end-user traffic is proxied.

---

## Before you start (Winhost control panel)

### Plan requirements

- [ ] **.NET Core / ASP.NET Core** hosting (Winhost supports .NET 10 — [KB](https://support.winhost.com/kb/a1498/installed-_net-core-frameworks.aspx))
- [ ] **One MS SQL** database on the plan (or purchased add-on)
- [ ] Two site targets: main domain (WASM) + subdomain (API), each with its own folder / app pool as needed

### Sites and DNS

- [ ] **Main domain** (or addon) for WASM static files
- [ ] **Subdomain** `api.` (recommended) for the API — separate IIS application folder
- [ ] DNS **A** or **CNAME** to Winhost; **proxied** (orange cloud) in Cloudflare for both hostnames

Do not put WASM and API in one IIS app unless you know the folder layout; subdomain + separate folder is simpler.

### SQL database

- [ ] Create one database (e.g. `KaraokeList`) in **MS SQL Manager**
- [ ] Save server name, database name, user, password
- [ ] Build connection string from panel template; add Winhost TLS settings ([KB](https://support.winhost.com/kb/a1729/problem-with-establishing-encrypted-connection-to-sql-server-from-your-application.aspx)):

```text
Encrypt=yes;TrustServerCertificate=true;MultipleActiveResultSets=true
```

The database can start **empty**. On first API startup:

1. EF Core Identity migrations
2. `scripts/azure-sql/001-karaoke-schema.sql` (catalog + `Performances`)

Catalog **data** (songs, artists, …) is loaded separately — see [deployment-roadmap.md](deployment-roadmap.md) Phase 1 (`MigrateSqliteToSqlServer`).

### Deploy credentials (for CI/CD later)

From **Site Info → Publishing Information**:

- [ ] **Web Deploy** (preferred for API)
- [ ] **FTP** (fine for WASM static upload)

Store in a password manager; GitHub Actions will use **repository secrets**, never git.

### Production secrets (generate now; configure on API at deploy)

| Setting | Purpose |
|---------|---------|
| `ConnectionStrings__DefaultConnection` | Winhost SQL |
| `Jwt__Key` | New 32+ char secret (not dev `appsettings.json` value) |
| `Security__Registration__InviteCode` | Friends-only registration |
| `Security__Registration__RequireInviteCode` | `true` |
| `Cors__Origins__0` | `https://<your-wasm-host>` (Cloudflare public URL) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

WASM needs production **`ApiBaseUrl`** → `https://api.<your-domain>/` (set at publish time in CI or `wwwroot/appsettings.json`).

See [security-private-access.md](security-private-access.md).

---

## One database — design choice

Winhost plans typically include **one MS SQL database per slot**. KaraokeList is already designed for **a single database** with one connection string (`DefaultConnection`).

### What lives in that database

| Area | Tables | Created by |
|------|--------|------------|
| Auth | `AspNetUsers`, `AspNetRoles`, … | EF migrations (API startup) |
| Catalog | `Genres`, `Artists`, `Singers`, `Venues`, `Songs` | `001-karaoke-schema.sql` |
| Performances | `Performances` | Same script |
| Link | `AspNetUsers.SingerId` → `Singers.Id` | EF migration |

All catalog and performance tables use the default **`dbo`** schema today. No empty “generic” database or custom schema layout is required before deploy.

### What you do **not** need

- A pre-provisioned “shell” database with manual table layout
- SQL **schemas** (`karaoke.*`, `identity.*`) for KaraokeList alone
- A second database on Winhost for this app only

### If you later host **another app** on the same SQL database

Only then consider stronger isolation:

| Approach | When |
|----------|------|
| **Second database** on Winhost (if plan/add-on allows) | Cleanest — `KaraokeList` + `OtherApp` |
| **Schema per app** (`otherapp.Invoices`, leave KaraokeList in `dbo`) | One DB slot, multiple apps; new app uses its own schema from day one |
| **MySQL** for the other app (if on plan) | KaraokeList stays on MS SQL |

Moving KaraokeList from `dbo` to a `karaoke` schema is possible but touches EF, SQL scripts, and all `*Service.cs` queries — only worth it if you standardize multi-app sharing.

### Dev vs prod

Use **separate databases on separate hosts**, not schemas in one production DB:

| Environment | Database |
|-------------|----------|
| Local | LocalDB `KaraokeList` |
| Winhost prod | Winhost SQL `KaraokeList` |
| Azure prod | Azure SQL `KaraokeList` (separate host) |

Sync catalog between environments with the migration tool when needed ([deployment-roadmap.md](deployment-roadmap.md)).

**Decision for this project:** one Winhost database, everything in `dbo`, same model as local dev and Azure.

---

## HTTPS — Cloudflare proxy + origin certificate

Winhost commercial SSL can be expensive. A common pattern (used for this project):

```text
External clients ──HTTPS──▶ Cloudflare edge
Cloudflare ──HTTPS (origin cert)──▶ Winhost IIS
```

Public URLs are always **`https://`** hostnames on Cloudflare. Browsers never talk to Winhost directly.

### Cloudflare SSL modes

| Mode | Cloudflare → Winhost | Notes |
|------|----------------------|--------|
| **Flexible** | HTTP to origin | Easiest; origin cert unused on IIS. API may see HTTP unless forwarded headers are configured. |
| **Full** | HTTPS with origin cert on IIS | Matches origin-cert setup. |
| **Full (strict)** | HTTPS; origin presents cert Cloudflare trusts | **Cloudflare origin certificate** works here. |

Prefer **Full** or **Full (strict)** when using a Cloudflare origin cert installed on Winhost IIS for both WASM and API sites.

### Origin certificate on Winhost

1. In Cloudflare: SSL/TLS → Origin Server → create **Origin Certificate**
2. Install cert + private key on Winhost IIS for each site (WASM + API), per Winhost/IIS docs
3. Bind HTTPS on IIS for those sites

### DNS

- WASM and **api.** subdomain: **proxied** (orange cloud)
- Ensures no mixed content (HTTPS page calling HTTP API)

### App configuration (public URLs)

| Config | Value |
|--------|--------|
| WASM `ApiBaseUrl` | `https://api.<your-domain>/` |
| API `Cors:Origins` | `https://<your-wasm-host>` |
| Smoke tests / bookmarks | Same `https://` URLs |

Local dev stays `http://localhost:5299` / `5262` — see [wasm-api-local-dev.md](wasm-api-local-dev.md).

### Cloudflare caching

| Path | Rule |
|------|------|
| WASM static (`/_framework/`, `.wasm`, `.dll`, `.css`, `.js`) | Cache friendly |
| `https://api.*/**` | **Bypass cache** (API must not be cached as static HTML) |

Use a Cache Rule or legacy Page Rule for the API hostname.

### ASP.NET Core behind Cloudflare

IIS may receive requests that look like HTTP while clients used HTTPS. If you see wrong redirects or scheme issues:

- Configure **forwarded headers** (`X-Forwarded-Proto`, `X-Forwarded-For`) on the API
- Review `UseHttpsRedirection()` behavior behind a proxy

WASM is static files only — no forwarded-header concern on the main site.

### CI/CD deploy target

| Traffic | Target |
|---------|--------|
| Users | `https://` Cloudflare hostnames |
| GitHub Actions Web Deploy / FTP | Winhost **origin** host from publishing profile (bypass Cloudflare) |

Record both in your password manager:

- Public WASM URL: `https://…`
- Public API URL: `https://api.…`
- Origin deploy hostname (from Winhost Site Info)

### Syncfusion license

Unrelated to Cloudflare. WASM key is embedded at **build** (`/p:SyncfusionKey` or user secrets), not read from Winhost at runtime. See [wasm-api-local-dev.md](wasm-api-local-dev.md).

---

## API configuration reference (production)

Example shape (values in Winhost panel / `appsettings.Production.json` on server — **not** in git):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=KaraokeList;User ID=...;Password=...;Encrypt=yes;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "KaraokeList",
    "Audience": "KaraokeList.Web",
    "Key": "<production-secret-32-chars-minimum>",
    "ExpirationHours": 8
  },
  "Cors": {
    "Origins": [ "https://<your-wasm-host>" ]
  },
  "Security": {
    "Registration": {
      "AllowRegistration": true,
      "RequireInviteCode": true,
      "InviteCode": "<your-invite-code>",
      "AllowPasswordRecovery": false
    }
  }
}
```

WASM production `wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "https://api.<your-domain>"
}
```

---

## Publish notes (manual or CI)

| Project | Command | Deploy target |
|---------|---------|---------------|
| API | `dotnet publish KaraokeList.Api -c Release` | Web Deploy → API site folder |
| WASM | `dotnet publish KaraokeList.Web -c Release /p:SyncfusionKey=...` | FTP/Web Deploy → `wwwroot` contents of main site |

- **Framework-dependent** deployment is supported for .NET 10 on Winhost.
- Default **InProcess** hosting is fine if the API subdomain has its own app pool.
- If multiple Core apps share one pool, Winhost may require **OutOfProcess** — see [Winhost KB](https://support.winhost.com/kb/a1604/visual-studio-publish-web-deploy.aspx).

---

## CI/CD (planned)

GitHub Free Actions is sufficient. Workflow: `deploy-winhost.yml` (manual `workflow_dispatch` recommended).

**Secrets:** `SYNCFUSION_KEY`, Web Deploy or FTP credentials, optional origin hostname.

Details and checklists: [deployment-roadmap.md](deployment-roadmap.md) Phase 3.

---

## Pre-flight checklist

| # | Task |
|---|------|
| 1 | .NET Core plan + one SQL database created |
| 2 | WASM hostname + `api.` subdomain; Cloudflare DNS proxied |
| 3 | Origin cert on IIS; SSL mode Full or Full (strict) |
| 4 | SQL connection string with `TrustServerCertificate=true` |
| 5 | Web Deploy / FTP credentials saved |
| 6 | Production JWT key + invite code generated |
| 7 | Public `https://` URLs recorded for CORS + `ApiBaseUrl` |
| 8 | Origin deploy hostname recorded for pipeline |
| 9 | API cache bypass rule on `api.*` in Cloudflare |

---

## Related docs

| Doc | Topic |
|-----|--------|
| [deployment-roadmap.md](deployment-roadmap.md) | Full roadmap, data migration, Azure |
| [wasm-api-local-dev.md](wasm-api-local-dev.md) | Local dev, Syncfusion |
| [security-private-access.md](security-private-access.md) | Invite code, registration |
| [Performances.md](Performances.md) | Schema and API |
