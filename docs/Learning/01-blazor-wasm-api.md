# 01 — Blazor WASM + API

## Overview

KaraokeList is a **split-hosting** application: a Blazor WebAssembly UI in the browser talks to a separate ASP.NET Core Web API. Shared DTOs live in `KaraokeList.Shared`. This architecture teaches CORS, JWT-over-HTTP, configuration-driven base URLs, and clean client/server boundaries.

| Project | Role |
|---------|------|
| `KaraokeList.Web` | Blazor WASM UI (mobile Log / My Songs + catalog grids) |
| `KaraokeList.Api` | REST API, Identity, EF Core, SQL |
| `KaraokeList.Shared` | DTOs and helpers used by both |

Local defaults: API `http://localhost:5299`, WASM `http://localhost:5262`.

## Major aspects

1. **WASM runs in the browser** — no server session for page state; everything that needs secrets or SQL goes through the API.
2. **`ApiBaseUrl` is configuration** — Development uses localhost; Azure deploy patches production URL into `wwwroot/appsettings.json`.
3. **Named `HttpClient` + message handlers** — Authorization (JWT), slow-request UI, and safe-read retries form a pipeline.
4. **Shared DTOs** — request/response shapes stay in sync without duplicating models.
5. **CORS on the API** — only known WASM origins may call the API from the browser.
6. **Auth is Bearer JWT** — login stores a token; handlers attach it to outbound calls.

## Code samples

### Sample 1 — WASM host and HttpClient pipeline

```22:66:KaraokeList.Web/Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is required in wwwroot/appsettings.json");
// ... DI registrations for auth, loaders, local stores ...
builder.Services.AddHttpClient("KaraokeApi", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromMinutes(2);
    })
    .AddHttpMessageHandler<SafeReadRetryHandler>()
    .AddHttpMessageHandler<SlowApiRequestHandler>()
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("KaraokeApi"));
```

### Sample 2 — Attach JWT to every API call

```6:17:KaraokeList.Web/Services/AuthorizationMessageHandler.cs
public sealed class AuthorizationMessageHandler(ILocalStorageService localStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await localStorage.GetItemAsStringAsync(JwtAuthenticationStateProvider.TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim('"'));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Api/Program.cs` | ~124–167 | CORS policy `WebClient`, authentication middleware, `MapControllers` |
| `KaraokeList.Web/wwwroot/appsettings.Development.json` | full file | Local `ApiBaseUrl` pointing at the API |
| `docs/wasm-api-local-dev.md` | full | End-to-end local setup (CORS, Syncfusion, OAuth redirect notes) |

## Exercises

1. **Multiple choice.** Which project holds the Blazor WASM UI?
   - A) `KaraokeList.Api`
   - B) `KaraokeList.Web`
   - C) `KaraokeList.Shared`
   - D) `KaraokeList.E2E`

2. **Fill in the blank.** The WASM app reads its API endpoint from the configuration key ________.

3. **Multiple choice.** Why does the WASM project need a separate API process?
   - A) Blazor cannot use SQL Server
   - B) Secrets, Identity, and database access belong on the server, not in browser-downloaded code
   - C) Syncfusion requires a separate host
   - D) GitHub Actions cannot build WASM alone

4. **Fill in the blank.** Shared request/response types live in the ________ project.

5. **Multiple choice.** `AuthorizationMessageHandler` derives from:
   - A) `HttpClient`
   - B) `AuthenticationStateProvider`
   - C) `DelegatingHandler`
   - D) `ControllerBase`

6. **Fill in the blank.** Browser-origin calls are allowed by the API’s ________ policy named `WebClient`.

7. **Multiple choice.** Handler registration order with `AddHttpMessageHandler` means the *last* registered handler is:
   - A) Closest to the network (innermost)
   - B) Closest to the caller (outermost)
   - C) Never executed
   - D) Only used for GET requests

8. **Fill in the blank.** Controllers are mapped on the API with ________.

9. **Multiple choice.** Which file typically holds the production `ApiBaseUrl` that Azure deploy patches?
   - A) `KaraokeList.Api/appsettings.json`
   - B) `KaraokeList.Web/wwwroot/appsettings.json`
   - C) `infra/main.bicep`
   - D) `.gitignore`

10. **Fill in the blank.** Local API default port in this project is ________; WASM default is ________.

## Answer key

1. B  
2. `ApiBaseUrl`  
3. B  
4. `KaraokeList.Shared`  
5. C  
6. CORS  
7. B  
8. `MapControllers()` (or `app.MapControllers()`)  
9. B  
10. `5299` ; `5262`  
