namespace KaraokeList.Security;

public sealed class RegistrationSettings
{
    public const string SectionName = "Security:Registration";

    /// <summary>When false, the Register page and nav link are hidden; only existing accounts can sign in.</summary>
    public bool AllowRegistration { get; set; } = true;

    /// <summary>When true, new accounts must supply the invite code (set via user secrets / Azure app settings).</summary>
    public bool RequireInviteCode { get; set; } = true;

    /// <summary>Shared secret you give friends out-of-band. Never commit a real value.</summary>
    public string? InviteCode { get; set; }

    /// <summary>Optional. If non-empty, only these email domains may register (e.g. "gmail.com").</summary>
    public string[] AllowedEmailDomains { get; set; } = [];

    /// <summary>When false, hides Forgot password / Resend confirmation to reduce bot noise.</summary>
    public bool AllowPasswordRecovery { get; set; } = false;
}
