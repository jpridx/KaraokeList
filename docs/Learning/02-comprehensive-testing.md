# 02 — Comprehensive Testing

## Overview

KaraokeList uses a layered test strategy so changes can be verified at the right level of fidelity: fast unit tests, Blazor component tests with **bUnit**, API integration tests against SQL Server, and browser **Playwright** E2E tests against a real WASM + API pair.

| Project | Stack | What it proves |
|---------|-------|----------------|
| `KaraokeList.Api.Tests` | xUnit | Pure API/shared logic |
| `KaraokeList.Web.Tests` | xUnit + **bUnit** + Moq | Blazor components & WASM services |
| `KaraokeList.Api.IntegrationTests` | `WebApplicationFactory` + SQL | HTTP + Identity + database contracts |
| `KaraokeList.E2E` | **Microsoft.Playwright.Xunit** | Real browser flows (auth, log, offline) |

CI runs the first three projects. Playwright E2E is currently local-first (see `docs/e2e-playwright.md`).

## Major aspects

1. **Test pyramid** — many fast unit/bUnit tests; fewer integration tests; sparsest E2E.
2. **bUnit** — renders Razor components in-memory, injects mocks, asserts markup/behavior without a browser.
3. **Integration tests** — spin up the real API host with a Testing environment and SQL; stub external side effects (email, MusicBrainz).
4. **Playwright** — drives Chromium against live `e2e` launch profiles; can auto-start Api + Web.
5. **Skippable E2E** — tests skip cleanly when servers are unavailable rather than failing the whole suite.
6. **Test doubles** — in-memory local storage, fake API clients, capturing email senders keep tests hermetic.

## Code samples

### Sample 1 — bUnit base context

```11:26:KaraokeList.Web.Tests/BunitTestContext.cs
public abstract class BunitTestContext : BunitContext
{
    protected BunitTestContext()
    {
        ConfigureServices(Services);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected void AddSyncfusionServices(IServiceCollection services)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        services.AddSyncfusionBlazor();
    }
}
```

### Sample 2 — Playwright auto-starts Api + Web

```18:54:KaraokeList.E2E/E2eServerFixture.cs
    public async Task InitializeAsync()
    {
        if (E2eConfiguration.ManualServers)
        {
            skipReason = await VerifyManualServersAsync();
            // ...
            return;
        }
        // ...
        startedProcesses.Add(StartDotnetProcess(
            repoRoot,
            "KaraokeList.Api/KaraokeList.Api.csproj",
            "--launch-profile e2e"));

        startedProcesses.Add(StartDotnetProcess(
            repoRoot,
            "KaraokeList.Web/KaraokeList.Web.csproj",
            "--launch-profile e2e"));

        skipReason = await WaitForServersAsync();
        // ...
    }
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Api.IntegrationTests/KaraokeApiFactory.cs` | ~14–63 | `WebApplicationFactory<Program>`, Testing env, stubbed email/rate limits |
| `KaraokeList.Web.Tests/Components/OfflineCacheNoticeTests.cs` | ~6–31 | Example bUnit render + assertion |
| `docs/e2e-playwright.md` | full | How to run Playwright, auth helpers, offline scenarios |

## Exercises

1. **Multiple choice.** Which library renders Blazor components in unit tests without a browser?
   - A) Playwright
   - B) bUnit
   - C) Moq
   - D) Polly

2. **Fill in the blank.** Full browser end-to-end tests live in the ________ project.

3. **Multiple choice.** `WebApplicationFactory<Program>` is primarily used for:
   - A) Publishing WASM to Azure
   - B) Hosting the API in-process for integration tests
   - C) Generating JWT keys
   - D) Starting Playwright browsers

4. **Fill in the blank.** The base class for Blazor component tests in this repo is ________.

5. **Multiple choice.** Which test projects run in the GitHub Actions CI workflow today?
   - A) Only E2E
   - B) Api.Tests, Web.Tests, and Api.IntegrationTests
   - C) Only Api.Tests
   - D) All four including E2E

6. **Fill in the blank.** Playwright tests that should not hard-fail when servers are down typically use ________ facts.

7. **Multiple choice.** Why do integration tests stub `IAccountEmailSender`?
   - A) Email is not part of Identity
   - B) To avoid sending real email while still exercising auth flows
   - C) SMTP is banned in Azure
   - D) bUnit cannot mock email

8. **Fill in the blank.** Syncfusion in bUnit tests usually needs `JSInterop.Mode = ________`.

9. **Multiple choice.** The E2E fixture starts projects with which launch profile?
   - A) `Development`
   - B) `Production`
   - C) `e2e`
   - D) `Integration`

10. **Fill in the blank.** Mocking library commonly paired with bUnit in `KaraokeList.Web.Tests` is ________.

## Answer key

1. B  
2. `KaraokeList.E2E`  
3. B  
4. `BunitTestContext`  
5. B  
6. `SkippableFact` (or skippable)  
7. B  
8. `JSRuntimeMode.Loose` (or `Loose`)  
9. C  
10. Moq  
