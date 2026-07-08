using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileTonightDashboardTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Tonight_dashboard_shows_recent_log_and_links_to_log_page()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var songTitle = $"E2E Tonight Song {Guid.NewGuid():N}";
        var (songId, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, songTitle);

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Expect(Page.GetByText($"Signed in as {servers.WarmUpEmail}")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GotoAsync($"/log?songId={songId}");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Log performance" })).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save performance" })).ToBeVisibleAsync(new() { Timeout = 120_000 });

        var venueName = $"E2E Venue {Guid.NewGuid():N}";
        await Page.GetByRole(AriaRole.Button, new() { Name = "+ New venue" }).ClickAsync();
        var venueInput = Page.Locator(".border-top.pt-3.mt-2 input.form-control");
        await venueInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await venueInput.FillAsync(venueName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add venue" }).ClickAsync();
        await Expect(Page.GetByText("Venue added.")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save performance" }).ClickAsync();
        await Expect(Page.GetByText("Performance saved.")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GotoAsync("/");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Tonight" })).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByText("Recently logged")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.Locator(".recent-log-item").Filter(new() { HasText = songTitle }).First).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.Locator(".recent-log-item").Filter(new() { HasText = songTitle }).First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"/log\\?songId={songId}"), new() { Timeout = 60_000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save performance" })).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
