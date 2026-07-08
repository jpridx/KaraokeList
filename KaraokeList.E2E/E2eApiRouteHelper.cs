using Microsoft.Playwright;

namespace KaraokeList.E2E;

internal static class E2eApiRouteHelper
{
    public static async Task BlockPerformanceCreatesAsync(IPage page)
    {
        var pattern = $"{E2eConfiguration.ApiBaseUrl}/api/performances**";
        await page.RouteAsync(pattern, async route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await route.AbortAsync("failed");
            }
            else
            {
                await route.ContinueAsync();
            }
        });
    }
}
