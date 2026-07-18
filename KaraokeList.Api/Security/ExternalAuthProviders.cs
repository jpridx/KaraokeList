using KaraokeList.Shared;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace KaraokeList.Security;

public static class ExternalAuthProviders
{
    public const string Google = ExternalAuthProviderNames.Google;
    public const string Microsoft = ExternalAuthProviderNames.Microsoft;

    public const string ReturnUrlItemKey = "returnUrl";
    public const string InviteItemKey = "invite";
    public const string RememberMeItemKey = "rememberMe";

    public static bool TryGetScheme(string provider, out string scheme)
    {
        scheme = provider.Trim().ToLowerInvariant() switch
        {
            Google => GoogleDefaults.AuthenticationScheme,
            Microsoft => MicrosoftAccountDefaults.AuthenticationScheme,
            _ => string.Empty
        };

        return scheme.Length > 0;
    }

    public static bool IsConfigured(AuthenticationSettings settings, string provider) =>
        provider.Trim().ToLowerInvariant() switch
        {
            Google => settings.Google.IsConfigured,
            Microsoft => settings.Microsoft.IsConfigured,
            _ => false
        };
}
