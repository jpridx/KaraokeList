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

    public static async Task WarmUpAuthenticatedCatalogAsync(HttpClient apiClient)
    {
        var (_, _, token) = await RegisterSingerAsync(apiClient);
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await apiClient.GetAsync("/api/performances/my-repertoire?includeAll=true");
        await apiClient.GetAsync("/api/genres");
    }
}
