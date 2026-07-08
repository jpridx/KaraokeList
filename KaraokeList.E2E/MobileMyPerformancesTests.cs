using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileMyPerformancesTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Authenticated_user_can_edit_a_performance_on_my_performances()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var songTitle = $"E2E Edit Perf {Guid.NewGuid():N}";
        var (songId, _, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, songTitle);
        await E2eCatalogHelper.SeedPerformanceAsync(apiClient, servers.WarmUpToken!, songId, performedOn: DateTime.Today);

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Page.GotoAsync("/my-performances");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My performances" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GetByPlaceholder("Search song, artist, or venue").FillAsync(songTitle);
        var performanceItem = Page.Locator(".performance-browse-item").Filter(new() { HasText = songTitle }).First;
        await Expect(performanceItem).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await performanceItem.Locator("button.btn-link", new() { HasText = "Edit" }).ClickAsync();

        var editForm = performanceItem.Locator(".history-edit-form");
        await editForm.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await Expect(editForm.GetByText("Date")).ToBeVisibleAsync();
        await Expect(editForm.GetByText("Venue")).ToBeVisibleAsync();
        await Expect(editForm.GetByText("Key")).ToBeVisibleAsync();

        await editForm.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(editForm).ToBeHiddenAsync(new() { Timeout = 60_000 });
        await Expect(performanceItem.Locator("button.performance-browse-main")).ToBeVisibleAsync();
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [SkippableFact]
    public async Task Authenticated_user_can_delete_a_performance_on_my_performances()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var songTitle = $"E2E Delete Perf {Guid.NewGuid():N}";
        var (songId, _, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, songTitle);
        await E2eCatalogHelper.SeedPerformanceAsync(apiClient, servers.WarmUpToken!, songId);

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Page.GotoAsync("/my-performances");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My performances" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GetByPlaceholder("Search song, artist, or venue").FillAsync(songTitle);
        var performanceItem = Page.Locator(".performance-browse-item").Filter(new() { HasText = songTitle }).First;
        await Expect(performanceItem).ToBeVisibleAsync(new() { Timeout = 120_000 });
        await performanceItem.Locator("button.btn-link.text-danger", new() { HasText = "Delete" }).ClickAsync();
        await Expect(performanceItem.GetByText("Delete this performance?")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await performanceItem.Locator("button.btn-danger", new() { HasText = "Delete" }).ClickAsync();

        await Expect(Page.Locator(".performance-browse-item").Filter(new() { HasText = songTitle })).ToHaveCountAsync(0, new() { Timeout = 60_000 });
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
