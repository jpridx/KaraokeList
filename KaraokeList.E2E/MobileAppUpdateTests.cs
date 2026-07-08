using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileAppUpdateTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task App_update_banner_appears_when_update_is_available()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Page.GotoAsync("/");
        await Expect(Page.GetByText($"Signed in as {servers.WarmUpEmail}")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.WaitForFunctionAsync(
            "() => window.karaokeListAppUpdates && window.karaokeListAppUpdates.dotNetRef !== null",
            null,
            new() { Timeout = 60_000 });
        await Page.EvaluateAsync("() => window.karaokeListAppUpdates.notifyUpdate()");

        await Expect(Page.Locator(".app-update-banner")).ToContainTextAsync("A new version of KaraokeList is ready.", new() { Timeout = 60_000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Refresh now" })).ToBeVisibleAsync();
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
