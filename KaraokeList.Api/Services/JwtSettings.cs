namespace KaraokeList.Api.Services;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "KaraokeList";
    public string Audience { get; set; } = "KaraokeList.Web";
    public string Key { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 8;
}
