using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace KaraokeList.Security;

public interface IRegistrationGate
{
    bool IsRegistrationOpen { get; }
    bool RequiresInviteCode { get; }
    bool IsPasswordRecoveryAllowed { get; }
    InviteShareAvailability GetInviteShareAvailability();
    RegistrationValidationResult ValidateRegistration(string email, string? inviteCode, string? honeypot);
}

public sealed record InviteShareAvailability(bool CanShare, string? InviteCode, string? UnavailableReason);

public sealed record RegistrationValidationResult(bool Allowed, string? ErrorMessage);

public sealed class RegistrationGate(IOptions<RegistrationSettings> options) : IRegistrationGate
{
    private readonly RegistrationSettings _settings = options.Value;

    public bool IsRegistrationOpen => _settings.AllowRegistration;

    public bool RequiresInviteCode => _settings.AllowRegistration && _settings.RequireInviteCode;

    public bool IsPasswordRecoveryAllowed => _settings.AllowPasswordRecovery;

    public InviteShareAvailability GetInviteShareAvailability()
    {
        if (!_settings.AllowRegistration)
        {
            return new InviteShareAvailability(false, null, "Registration is closed. Turn it back on in server settings to invite new friends.");
        }

        if (!_settings.RequireInviteCode)
        {
            return new InviteShareAvailability(false, null, "Registration does not require an invite code. Share the site URL and Register page instead.");
        }

        if (string.IsNullOrWhiteSpace(_settings.InviteCode))
        {
            return new InviteShareAvailability(false, null, "Invite code is not configured on the server.");
        }

        return new InviteShareAvailability(true, _settings.InviteCode, null);
    }

    public RegistrationValidationResult ValidateRegistration(string email, string? inviteCode, string? honeypot)
    {
        if (!_settings.AllowRegistration)
        {
            return new(false, "New accounts are not being accepted.");
        }

        if (!string.IsNullOrWhiteSpace(honeypot))
        {
            return new(false, "Unable to create account. Please try again later.");
        }

        if (_settings.RequireInviteCode)
        {
            var configured = _settings.InviteCode;
            if (string.IsNullOrWhiteSpace(configured))
            {
                return new(false, "Registration is not configured. Contact the site owner.");
            }

            if (string.IsNullOrWhiteSpace(inviteCode) || !FixedTimeEquals(inviteCode.Trim(), configured))
            {
                return new(false, "Invalid invite code.");
            }
        }

        if (_settings.AllowedEmailDomains is { Length: > 0 })
        {
            var at = email.LastIndexOf('@');
            if (at < 0)
            {
                return new(false, "Enter a valid email address.");
            }

            var domain = email[(at + 1)..].Trim().ToLowerInvariant();
            var allowed = _settings.AllowedEmailDomains
                .Select(d => d.Trim().ToLowerInvariant())
                .Where(d => d.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

            if (!allowed.Contains(domain))
            {
                return new(false, "That email domain is not allowed to register.");
            }
        }

        return new(true, null);
    }

    private static bool FixedTimeEquals(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
