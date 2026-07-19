using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace KaraokeList.Api.Services;

public sealed record ExternalAuthCodeEntry(string UserId, bool RememberMe);

public interface IExternalAuthCodeStore
{
    string CreateCode(string userId, bool rememberMe);

    ExternalAuthCodeEntry? ConsumeCode(string code);
}

/// <summary>Short-lived one-time codes for exchanging OAuth success for a JWT (in-memory; resets on restart).</summary>
public sealed class ExternalAuthCodeStore : IExternalAuthCodeStore
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, (ExternalAuthCodeEntry Entry, DateTime ExpiresUtc)> _codes = new();

    public string CreateCode(string userId, bool rememberMe)
    {
        PurgeExpired();
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        _codes[code] = (new ExternalAuthCodeEntry(userId, rememberMe), DateTime.UtcNow.Add(CodeLifetime));
        return code;
    }

    public ExternalAuthCodeEntry? ConsumeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        PurgeExpired();
        if (!_codes.TryRemove(code, out var stored) || stored.ExpiresUtc < DateTime.UtcNow)
        {
            return null;
        }

        return stored.Entry;
    }

    private void PurgeExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _codes.Keys)
        {
            if (_codes.TryGetValue(key, out var stored) && stored.ExpiresUtc < now)
            {
                _codes.TryRemove(key, out _);
            }
        }
    }
}
