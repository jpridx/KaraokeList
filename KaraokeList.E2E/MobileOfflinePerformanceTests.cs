using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileOfflinePerformanceTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Offline_save_queues_performance_and_sync_now_persists_it()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var (songId, songTitle, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!);

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
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

        await E2eApiRouteHelper.BlockPerformanceCreatesAsync(Page);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save performance" }).ClickAsync();
        await Expect(Page.GetByText("Saved on this device. Will sync when you're back online."))
            .ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GotoAsync("/more");
        await Expect(Page.Locator(".pending-sync-banner")).ToContainTextAsync("1 performance waiting to sync.", new() { Timeout = 60_000 });

        await Page.UnrouteAllAsync();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (await E2eCatalogHelper.PerformanceExistsForSongAsync(apiClient, servers.WarmUpToken!, songTitle))
            {
                break;
            }

            await Page.GotoAsync(attempt % 2 == 0 ? "/log" : "/more");
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.True(await E2eCatalogHelper.PerformanceExistsForSongAsync(apiClient, servers.WarmUpToken!, songTitle));
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
