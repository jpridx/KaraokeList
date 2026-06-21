namespace KaraokeList.Shared;

public static class InviteShareFormatting
{
    public static string BuildRegisterUrl(string siteBaseUri, string inviteCode)
    {
        var baseUri = siteBaseUri.TrimEnd('/');
        return $"{baseUri}/register?invite={Uri.EscapeDataString(inviteCode)}";
    }

    public static string BuildShareMessage(string registerUrl, string inviteCode, string appName = "KaraokeList")
    {
        return $"""
            Join me on {appName} to log and browse your karaoke repertoire!

            {registerUrl}

            Invite code: {inviteCode}
            """.Trim();
    }
}
