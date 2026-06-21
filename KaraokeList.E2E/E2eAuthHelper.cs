using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KaraokeList.Shared;
using Microsoft.Playwright;

namespace KaraokeList.E2E;

internal static class E2eAuthHelper
{
    public static async Task<(string Email, string Password, string Token)> RegisterSingerAsync(HttpClient apiClient)
    {
        var email = $"e2e-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest
        {
            Name = "E2E Test Singer",
            Email = email,
            Password = E2eConfiguration.TestPassword,
            ConfirmPassword = E2eConfiguration.TestPassword,
            InviteCode = E2eConfiguration.TestInviteCode
        };

        var response = await apiClient.PostAsJsonAsync("/api/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Register failed ({(int)response.StatusCode}): {body}");
        }

        var auth = JsonSerializer.Deserialize<AuthResponse>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (auth is null || string.IsNullOrWhiteSpace(auth.Token))
        {
            throw new InvalidOperationException("Register returned no token.");
        }

        return (email, E2eConfiguration.TestPassword, auth.Token);
    }

    public static async Task SignInViaLocalStorageAsync(IPage page, string token)
    {
        await page.GotoAsync("/");
        // Blazored.LocalStorage JSON-serializes string values.
        await page.EvaluateAsync(
            "(token) => localStorage.setItem('authToken', JSON.stringify(token))",
            token);
        await page.ReloadAsync();
    }

    public static async Task<(string Email, string Token)> RegisterAndSignInAsync(IPage page, HttpClient apiClient)
    {
        var (email, _, token) = await RegisterSingerAsync(apiClient);
        await SignInViaLocalStorageAsync(page, token);
        return (email, token);
    }

    public static async Task SignInViaLoginFormAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/login");
        await page.WaitForSelectorAsync("button:has-text('Sign in')", new() { Timeout = 60_000 });
        await page.Locator("input.form-control").First.FillAsync(email);
        await page.Locator("input[type='password']").FillAsync(password);

        var signInButton = page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
        var signedIn = page.GetByText($"Signed in as {email}");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await signInButton.ClickAsync();

            try
            {
                await signedIn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
                return;
            }
            catch (TimeoutException) when (attempt < 3)
            {
                if (!await signInButton.IsVisibleAsync())
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        throw new TimeoutException($"Sign-in did not reach home for {email} after 3 attempts.");
    }

    public static async Task<(string Email, string Token)> WarmUpApiAsync(HttpClient apiClient)
    {
        var (email, password, token) = await RegisterSingerAsync(apiClient);

        var loginResponse = await apiClient.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        if (!loginResponse.IsSuccessStatusCode)
        {
            var body = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login warm-up failed ({(int)loginResponse.StatusCode}): {body}");
        }

        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await apiClient.GetAsync("/api/performances/my-repertoire?includeAll=true");
        await apiClient.GetAsync("/api/genres");
        return (email, token);
    }
}
