using System.Text.RegularExpressions;
using KaraokeList.Shared;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileMySongsFlowTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Authenticated_user_can_open_song_detail_and_log_again()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var songTitle = $"E2E Detail Song {Guid.NewGuid():N}";
        var (songId, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, songTitle);
        await E2eCatalogHelper.SeedPerformanceAsync(apiClient, servers.WarmUpToken!, songId);

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Expect(Page.GetByText($"Signed in as {servers.WarmUpEmail}")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GotoAsync("/my-songs");
        await Expect(Page.GetByPlaceholder("Search title or artist")).ToBeVisibleAsync(new() { Timeout = 120_000 });

        var songRow = Page.Locator(".song-list-item-body").Filter(new() { HasText = songTitle }).First;
        await Expect(songRow).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await songRow.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($"/my-songs/{songId}"), new() { Timeout = 60_000 });
        await Expect(Page.GetByText(songTitle).First).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByText("Performance history (1)")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.Locator("summary.history-details-summary", new() { HasText = "Log again" }).ClickAsync();

        var venueName = $"E2E Venue {Guid.NewGuid():N}";
        await Page.GetByRole(AriaRole.Button, new() { Name = "+ New venue" }).ClickAsync();
        var venueInput = Page.Locator(".border-top.pt-3.mt-2 input.form-control");
        await venueInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await venueInput.FillAsync(venueName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add venue" }).ClickAsync();
        await Expect(Page.GetByText("Venue added.")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Log again" }).ClickAsync();
        await Expect(Page.GetByText("Performance history (2)")).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [SkippableFact]
    public async Task My_songs_switches_between_repertoire_want_to_sing_and_working_up()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl) };
        var repertoireTitle = $"E2E Repertoire {Guid.NewGuid():N}";
        var wantTitle = $"E2E Want {Guid.NewGuid():N}";
        var workingTitle = $"E2E Working {Guid.NewGuid():N}";

        var (repertoireSongId, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, repertoireTitle);
        await E2eCatalogHelper.SeedPerformanceAsync(apiClient, servers.WarmUpToken!, repertoireSongId);

        var (wantSongId, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, wantTitle);
        var (workingSongId, _) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!, workingTitle);
        await E2eCatalogHelper.AddSongsToListAsync(apiClient, servers.WarmUpToken!, SingerListKind.WantToSing, [wantSongId]);
        await E2eCatalogHelper.AddSongsToListAsync(apiClient, servers.WarmUpToken!, SingerListKind.WorkingUp, [workingSongId]);

        await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
        await Page.GotoAsync("/my-songs");
        await Expect(Page.GetByPlaceholder("Search title or artist")).ToBeVisibleAsync(new() { Timeout = 120_000 });

        await Page.Locator(".genre-chip", new() { HasText = "Want to sing" }).ClickAsync();
        await Expect(Page.GetByText(wantTitle)).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByText(repertoireTitle)).ToBeHiddenAsync();
        await Expect(Page.GetByText(workingTitle)).ToBeHiddenAsync();

        await Page.Locator(".genre-chip", new() { HasText = "Working up" }).ClickAsync();
        await Expect(Page.GetByText(workingTitle)).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByText(wantTitle)).ToBeHiddenAsync();
        await Expect(Page.GetByText(repertoireTitle)).ToBeHiddenAsync();

        await Page.Locator(".genre-chip", new() { HasText = "My repertoire" }).ClickAsync();
        await Expect(Page.GetByText(repertoireTitle)).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await Expect(Page.GetByText(wantTitle)).ToBeHiddenAsync();
        await Expect(Page.GetByText(workingTitle)).ToBeHiddenAsync();
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }
}
