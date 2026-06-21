using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileAuthFlowTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Login_page_loads_for_anonymous_user()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);

        await Page.GotoAsync("/login");
        await Page.WaitForSelectorAsync("button:has-text('Sign in')", new() { Timeout = 60_000 });

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Login" })).ToBeVisibleAsync();
    }

    [SkippableFact]
    public async Task Authenticated_user_can_open_my_songs()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var (email, _, token) = await E2eAuthHelper.RegisterSingerAsync(apiClient);

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, token);

        await Expect(Page.GetByText($"Signed in as {email}")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.Locator(".mobile-bottom-nav a", new() { HasText = "My Songs" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Songs" })).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByPlaceholder("Search title or artist")).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
