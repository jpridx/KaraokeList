using System.Net.Http.Json;
using KaraokeList.Shared;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileAddSongTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Authenticated_user_can_add_new_song_and_log_it()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        var songTitle = $"E2E New Song {Guid.NewGuid():N}";
        var artistName = $"E2E New Artist {Guid.NewGuid():N}";

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Expect(Page.GetByText($"Signed in as {servers.WarmUpEmail}")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GotoAsync("/log");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Log performance" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "+ New song" }).ClickAsync();
        var newSongPanel = Page.Locator(".log-new-song-panel");
        await newSongPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        await newSongPanel.Locator("input.form-control").First.FillAsync(songTitle);

        var artistInput = newSongPanel.Locator("input.e-input").First;
        await artistInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await artistInput.ClickAsync();
        await artistInput.FillAsync(string.Empty);
        await artistInput.PressSequentiallyAsync(artistName, new() { Delay = 100 });
        await newSongPanel.Locator("label.form-label", new() { HasText = "Title" }).ClickAsync();

        var addArtistButton = newSongPanel.Locator("button.btn-outline-primary", new() { HasText = "Add artist" });
        await Expect(addArtistButton).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await addArtistButton.ClickAsync();
        await Expect(newSongPanel.GetByText("Artist selected.")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.Keyboard.PressAsync("Escape");
        await newSongPanel.GetByRole(AriaRole.Button, new() { Name = "Add song" }).ClickAsync();

        var songId = await WaitForSongIdAsync(apiClient, servers.WarmUpToken!, songTitle);
        await Page.EvaluateAsync("() => localStorage.removeItem('karaoke.log.cachedCatalog')");
        await Page.GotoAsync($"/log?songId={songId}");
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
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
    public async Task Authenticated_user_can_add_new_song_to_working_up_from_my_songs()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        var songTitle = $"E2E My Songs Add {Guid.NewGuid():N}";
        var artistName = $"E2E My Songs Artist {Guid.NewGuid():N}";

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Expect(Page.GetByText($"Signed in as {servers.WarmUpEmail}")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GotoAsync("/my-songs");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Songs" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Working up" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "+ Add song" }).ClickAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "+ New song" }).ClickAsync();
        var newSongPanel = Page.Locator(".log-new-song-panel");
        await newSongPanel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        await newSongPanel.Locator("input.form-control").First.FillAsync(songTitle);

        var artistInput = newSongPanel.Locator("input.e-input").First;
        await artistInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await artistInput.ClickAsync();
        await artistInput.FillAsync(string.Empty);
        await artistInput.PressSequentiallyAsync(artistName, new() { Delay = 100 });
        await newSongPanel.Locator("label.form-label", new() { HasText = "Title" }).ClickAsync();

        var addArtistButton = newSongPanel.Locator("button.btn-outline-primary", new() { HasText = "Add artist" });
        await Expect(addArtistButton).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await addArtistButton.ClickAsync();
        await Expect(newSongPanel.GetByText("Artist selected.")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.Keyboard.PressAsync("Escape");
        await newSongPanel.GetByRole(AriaRole.Button, new() { Name = "Add song" }).ClickAsync();

        await Expect(Page.GetByText("Add to lists")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add to selected lists" }).ClickAsync();
        await Expect(Page.GetByText("added to Working up", new() { Exact = false })).ToBeVisibleAsync(new() { Timeout = 60_000 });

        var songId = await WaitForSongIdAsync(apiClient, servers.WarmUpToken!, songTitle);
        await E2eCatalogHelper.AssertSongOnListAsync(apiClient, servers.WarmUpToken!, SingerListKind.WorkingUp, songId);
    }

    private static async Task<int> WaitForSongIdAsync(HttpClient apiClient, string token, string songTitle)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                return await E2eCatalogHelper.FindSongIdByTitleAsync(apiClient, token, songTitle);
            }
            catch (InvalidOperationException) when (attempt < 59)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        throw new InvalidOperationException($"Song '{songTitle}' was not created within 60 seconds.");
    }
}
