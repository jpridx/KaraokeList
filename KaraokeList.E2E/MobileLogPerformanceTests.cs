using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileLogPerformanceTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Authenticated_user_can_log_a_performance()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        var (songId, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!);

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
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [SkippableFact]
    public async Task Log_song_picker_matches_dont_query_for_apostrophe_title()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);

        var searchableTitle = $"Don't E2E Song {Guid.NewGuid():N}";
        var (_, songTitle) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, searchableTitle);

        await Page.GotoAsync("/log");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Log performance" }))
            .ToBeVisibleAsync(new() { Timeout = 60_000 });

        var songInput = Page.Locator("input[role='combobox']").First;
        await songInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await songInput.ClickAsync();
        await songInput.FillAsync("dont");

        var matchingItem = Page.Locator(".e-list-item").Filter(new() { HasText = songTitle }).First;
        await Expect(matchingItem).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await matchingItem.ClickAsync();

        await Expect(songInput).ToHaveValueAsync(songTitle, new() { Timeout = 60_000 });
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Save performance" }))
            .ToBeVisibleAsync(new() { Timeout = 60_000 });
    }
}
