# Blazor WASM + API local development

## Projects

| Project | Role |
|---------|------|
| `KaraokeList.Web` | Blazor WebAssembly UI (Syncfusion Grid + DropDowns only) |
| `KaraokeList.Api` | ASP.NET Core Web API, EF Identity, SQL catalog |
| `KaraokeList.Shared` | DTOs shared between Web and Api |

## Run locally

One-time (if the browser warns about the dev certificate):

```powershell
dotnet dev-certs https --trust
```

Terminal 1 — API:

```powershell
dotnet run --project KaraokeList.Api
```

Default **https** profile: **https://localhost:5299** (same port as before, but TLS).

Apply schema (first time or after migration changes):

```powershell
dotnet ef database update --project KaraokeList.Api
```

See [database.md](database.md) for seed data.

Terminal 2 — WASM client:

```powershell
dotnet run --project KaraokeList.Web
```

Default **https** profile: **https://localhost:7262**.  
`wwwroot/appsettings.Development.json` sets `ApiBaseUrl` to **https://localhost:5299**.

**Cursor / VS Code:** `.vscode/launch.json` must use `launchSettingsProfile: "https"` for the API (already set). Without that, the debugger starts the **http** profile and `https://localhost:5299` fails with `ERR_SSL_PROTOCOL_ERROR`.

HTTP-only fallback: `dotnet run --project KaraokeList.Api --launch-profile http` and set `ApiBaseUrl` to `http://localhost:5299`.

## Syncfusion packages (Web only)

Pinned to **33.1.44** (must match your license key version and the CDN URL in `wwwroot/index.html`):

- `Syncfusion.Blazor.Grid` 33.1.44
- `Syncfusion.Blazor.DropDowns` 33.1.44

Theme CSS is loaded from the Syncfusion CDN (`fluent2-lite.css`), not a NuGet package — avoids bundling ~200MB of unused theme files.

Sample/demo pages from the old Server template are not included.

### Remove the trial license banner

The yellow Syncfusion popup means no license key was registered. **Do not put the key in `wwwroot` or any tracked file** — those JSON files are served to the browser and can be committed by mistake.

Use **.NET User Secrets** (stored under your user profile, outside the repo). Blazor WASM cannot read user secrets in the browser, so each **build** generates a gitignored `SyncfusionLicenseKey.g.cs` from your secrets (see `SyncfusionLicense.targets`). The key is compiled into the app — it does not appear as `appsettings.secrets.json` in the Network tab.

1. Get a key from [Syncfusion Community License](https://www.syncfusion.com/sales/communitylicense) or your [Syncfusion account downloads](https://www.syncfusion.com/account/downloads).
2. From the repo root:
   ```powershell
   dotnet user-secrets set "SyncfusionKey" "<your-license-key>" --project KaraokeList.Web
   ```
3. **Rebuild** (required after changing secrets):
   ```powershell
   dotnet build KaraokeList.Web/KaraokeList.Web.csproj
   dotnet run --project KaraokeList.Web
   ```
4. Hard-refresh the browser (Ctrl+F5).

To verify: `dotnet user-secrets list --project KaraokeList.Web`. The generated `SyncfusionLicenseKey.g.cs` is gitignored and must never be committed.

Shortcut (sets secrets, pins package version, rebuilds):

```powershell
.\scripts\set-syncfusion-key.ps1 -Key "<your-key>" -PackageVersion 33.1.44
```

For CI/production, pass `/p:SyncfusionKey=...` at build time instead of user secrets. Note: the key is still present in the published WASM client (normal for Syncfusion in the browser).

## Mobile UX

Singer-facing flows (Log, My Songs, copy-for-host): [mobile-ux.md](mobile-ux.md).

## Auth

- Register/login call `api/auth/register` and `api/auth/login`
- Optional OAuth (Google, Microsoft): `GET api/auth/external/{provider}`, `POST api/auth/external/exchange`, `GET api/auth/external/providers` — see [OAuth setup](#oauth-google--microsoft) below
- `GET api/auth/me` — profile and linked `SingerId`
- `POST api/auth/link-singer` — link login to an existing or new singer
- JWT is stored in browser local storage and sent on catalog API calls
- Development: invite code not required (`KaraokeList.Api/appsettings.Development.json`)

### OAuth (Google / Microsoft)

OAuth runs on the **API** (not WASM). After the provider redirects back, the API issues a short-lived exchange code; WASM completes sign-in at `/auth/callback`.

1. Create OAuth apps in [Google Cloud Console](https://console.cloud.google.com/apis/credentials) and/or [Azure Entra ID](https://entra.microsoft.com/) (App registrations).
2. Set authorized redirect URIs on the **API** host:
   - `https://localhost:5299/signin-google`
   - `https://localhost:5299/signin-microsoft`  
   (Use `http://localhost:5299/...` if you run the API HTTP-only.)
3. Store secrets with user secrets (never commit):

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "<google-client-id>" --project KaraokeList.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "<google-secret>" --project KaraokeList.Api
dotnet user-secrets set "Authentication:Microsoft:ClientId" "<microsoft-client-id>" --project KaraokeList.Api
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<microsoft-secret>" --project KaraokeList.Api
```

4. Restart the API. **Sign in with Google/Microsoft** buttons appear on Login/Register when a provider is configured.

New OAuth sign-ups follow the same **invite code** and **registration closed** rules as email/password registration. Pass `?invite=` on the OAuth start URL (Register page and invite links do this automatically).

Apple Sign In is planned as a follow-up; not configured in this release.
