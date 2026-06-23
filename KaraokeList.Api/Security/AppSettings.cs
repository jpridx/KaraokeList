namespace KaraokeList.Security;

public sealed class AppSettings
{
    public const string SectionName = "App";

    /// <summary>Public WASM origin used in password-reset links (no trailing slash).</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5262";
}
