namespace KaraokeList.Security;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@example.com";
    public string FromName { get; set; } = "KaraokeList";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost);
}
