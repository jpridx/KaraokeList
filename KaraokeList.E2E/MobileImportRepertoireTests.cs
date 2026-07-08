using System.Text;
using KaraokeList.Shared;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace KaraokeList.E2E;

[Collection(E2eCollection.Name)]
public sealed class MobileImportRepertoireTests(E2eServerFixture servers) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = E2eConfiguration.WebBaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 }
    };

    [SkippableFact]
    public async Task Authenticated_user_can_import_csv_into_my_repertoire()
    {
        Skip.IfNot(servers.IsReady, servers.SkipReason);
        Skip.If(servers.WarmUpToken is null, "Warm-up user was not created.");

        using var apiClient = new HttpClient { BaseAddress = new Uri(E2eConfiguration.ApiBaseUrl), Timeout = TimeSpan.FromMinutes(3) };
        var (_, songTitle, artistName) = await E2eCatalogHelper.SeedSongAsync(apiClient, servers.WarmUpToken!);

        var csvContent = $"Song,Artist,Genre,Year{Environment.NewLine}{songTitle},{artistName}";
        var csvPath = Path.Combine(Path.GetTempPath(), $"e2e-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(csvPath, csvContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        try
        {
            await E2eAuthHelper.SignInViaLocalStorageAsync(Page, servers.WarmUpToken!);
            await Page.GotoAsync("/import-repertoire");
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Import to list" })).ToBeVisibleAsync(new() { Timeout = 60_000 });

            await Page.Locator("select.form-select").SelectOptionAsync(nameof(SingerListKind.MyRepertoire));
            var fileInput = Page.Locator("input[type='file']");
            await fileInput.SetInputFilesAsync(csvPath);
            await fileInput.EvaluateAsync("el => { el.dispatchEvent(new Event('input', { bubbles: true })); el.dispatchEvent(new Event('change', { bubbles: true })); }");
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Import file" })).ToBeEnabledAsync(new() { Timeout = 60_000 });
            await Page.GetByRole(AriaRole.Button, new() { Name = "Import file" }).ClickAsync();
            await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Import results" })).ToBeVisibleAsync(new() { Timeout = 180_000 });

            var resultsSection = Page.Locator(".more-section").Filter(new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = "Import results" }) });
            await Expect(resultsSection.Locator("dt", new() { HasText = "Matched catalog" }).Locator("xpath=following-sibling::dd[1]"))
                .ToHaveTextAsync("1", new() { Timeout = 60_000 });
            await Expect(resultsSection.Locator("dt", new() { HasText = "Added to list" }).Locator("xpath=following-sibling::dd[1]"))
                .ToHaveTextAsync("1", new() { Timeout = 60_000 });

            await resultsSection.GetByRole(AriaRole.Button, new() { Name = "View My repertoire" }).ClickAsync();
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/my-songs$"), new() { Timeout = 60_000 });
            await Expect(Page.Locator(".song-list-item-body").Filter(new() { HasText = songTitle })).ToBeVisibleAsync(new() { Timeout = 120_000 });
            await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        }
        finally
        {
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }
    }
}
