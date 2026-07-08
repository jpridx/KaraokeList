using KaraokeList.Shared;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileCopyForHostTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Log_page_copy_for_host_copies_formatted_message()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var (songId, songTitle, artistName) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!);

        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Page.GotoAsync($"/log?songId={songId}");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Log performance" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        var expectedMessage = ShowHostMessageFormatting.Format(songTitle, artistName, keyChangeSemitones: null);
        await Expect(Page.Locator(".host-message-text")).ToHaveTextAsync(expectedMessage, new() { Timeout = 60_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Copy for host" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Copied!" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        var clipboardText = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        Assert.Equal(expectedMessage, clipboardText);
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
