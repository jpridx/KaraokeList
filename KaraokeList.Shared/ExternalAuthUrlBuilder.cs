using System.Text;

namespace KaraokeList.Shared;

public static class ExternalAuthProviderNames
{
    public const string Google = "google";
    public const string Microsoft = "microsoft";
}

public static class ExternalAuthUrlBuilder
{
    public static string BuildStartUrl(
        string apiBaseUrl,
        string provider,
        string? returnUrl = null,
        string? invite = null,
        bool rememberMe = false)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            query.Add($"returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        if (!string.IsNullOrWhiteSpace(invite))
        {
            query.Add($"invite={Uri.EscapeDataString(invite)}");
        }

        if (rememberMe)
        {
            query.Add("rememberMe=true");
        }

        var startUrl = $"{apiBaseUrl.TrimEnd('/')}/api/auth/external/{provider}";
        if (query.Count == 0)
        {
            return startUrl;
        }

        return $"{startUrl}?{string.Join('&', query)}";
    }
}
