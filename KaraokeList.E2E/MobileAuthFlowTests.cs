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
    public async Task User_can_sign_in_through_login_form()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var (email, password, _) = await E2eAuthHelper.RegisterSingerAsync(apiClient);

        await Page.GotoAsync("/");
        await Page.EvaluateAsync("() => localStorage.clear()");
        await E2eAuthHelper.SignInViaLoginFormAsync(Page, email, password);

        await Expect(Page.GetByText($"Signed in as {email}")).ToBeVisibleAsync();
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [SkippableFact]
    public async Task Authenticated_user_can_open_my_songs()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);

        await Expect(Page.GetByText($"Signed in as {servers.WarmUpEmail}")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.Locator(".mobile-bottom-nav a", new() { HasText = "My Songs" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Songs" })).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByPlaceholder("Search title or artist")).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
