namespace KaraokeList.Security;

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public OAuthProviderSettings Google { get; set; } = new();

    public OAuthProviderSettings Microsoft { get; set; } = new();
}

public sealed class OAuthProviderSettings
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
