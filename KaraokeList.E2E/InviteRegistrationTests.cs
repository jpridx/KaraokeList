using KaraokeList.Shared;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class InviteRegistrationTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Invite_link_allows_a_friend_to_register()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);

        await Page.GotoAsync("/invite-friends");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Invite friends" })).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Copy link" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        var registerUrl = InviteShareFormatting.BuildRegisterUrl(
            E2eConfiguration.WebBaseUrl + "/",
            E2eConfiguration.TestInviteCode);
        await Expect(Page.Locator(".invite-share-panel p.invite-share-text").First).ToHaveTextAsync(registerUrl);

        await using var friendContext = await Browser.NewContextAsync(ContextOptions());
        var friendPage = await friendContext.NewPageAsync();
        await friendPage.GotoAsync(registerUrl);
        await friendPage.WaitForSelectorAsync("button:has-text('Create account')", new() { Timeout = 60_000 });
        await Expect(friendPage.GetByRole(AriaRole.Heading, new() { Name = "Register" })).ToBeVisibleAsync();

        var friendEmail = $"e2e-friend-{Guid.NewGuid():N}@example.com";
        var fields = friendPage.Locator("input.form-control:visible");
        await fields.Nth(0).FillAsync("E2E Friend");
        await fields.Nth(1).FillAsync(friendEmail);
        await fields.Nth(2).FillAsync(E2eConfiguration.TestInviteCode);
        await friendPage.Locator("input[type='password']").First.FillAsync(E2eConfiguration.TestPassword);
        await friendPage.Locator("input[type='password']").Last.FillAsync(E2eConfiguration.TestPassword);
        await friendPage.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

        await Expect(friendPage.GetByText($"Signed in as {friendEmail}")).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Expect(friendPage.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
