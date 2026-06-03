# Blazor WASM + API local development

## Projects

| Project | Role |
|---------|------|
| `KaraokeList.Web` | Blazor WebAssembly UI (Syncfusion Grid + DropDowns only) |
| `KaraokeList.Api` | ASP.NET Core Web API, EF Identity, SQL catalog |
| `KaraokeList.Shared` | DTOs shared between Web and Api |
| `KaraokeList` | Legacy Blazor Server app (reference; not used for WASM path) |

## Run locally

Terminal 1 — API:

```powershell
cd KaraokeList.Api
dotnet run --launch-profile http
```

Listens on **http://localhost:5299**.

Terminal 2 — WASM client:

```powershell
cd KaraokeList.Web
dotnet run
```

Listens on **http://localhost:5262** (or the port in `Properties/launchSettings.json`).  
`wwwroot/appsettings.json` points `ApiBaseUrl` at the API.

## Syncfusion packages (Web only)

- `Syncfusion.Blazor.Grid` (includes grid date editing)
- `Syncfusion.Blazor.DropDowns` (dropdowns + autocomplete)
- `Syncfusion.Blazor.Themes`

Sample/demo pages from the old Server template are not included.

## Auth

- Register/login call `api/auth/register` and `api/auth/login`
- JWT is stored in browser local storage and sent on catalog API calls
- Development: invite code not required (`KaraokeList.Api/appsettings.Development.json`)
