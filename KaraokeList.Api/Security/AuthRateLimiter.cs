using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace KaraokeList.Security;

public interface IAuthRateLimiter
{
    bool AllowAttempt(string action, string clientKey, int maxAttempts, TimeSpan window);
}

/// <summary>Per-client fixed-window limits for login and registration (in-memory; resets on app restart).</summary>
public sealed class AuthRateLimiter(IMemoryCache cache) : IAuthRateLimiter
{
    private static readonly ConcurrentDictionary<string, object> Locks = new();

    public bool AllowAttempt(string action, string clientKey, int maxAttempts, TimeSpan window)
    {
        if (string.IsNullOrWhiteSpace(clientKey))
        {
            clientKey = "unknown";
        }

        var cacheKey = $"auth-limit:{action}:{clientKey}";
        var gate = Locks.GetOrAdd(cacheKey, _ => new object());

        lock (gate)
        {
            var count = cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = window;
                return 0;
            });

            if (count >= maxAttempts)
            {
                return false;
            }

            cache.Set(cacheKey, count + 1, window);
            return true;
        }
    }
}

public static class AuthRateLimitPolicies
{
    public static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(15);
    public const int LoginMaxAttempts = 10;

    public static readonly TimeSpan RegisterWindow = TimeSpan.FromHours(1);
    public const int RegisterMaxAttempts = 5;

    public static readonly TimeSpan ChangePasswordWindow = TimeSpan.FromMinutes(15);
    public const int ChangePasswordMaxAttempts = 5;

    public static readonly TimeSpan ForgotPasswordWindow = TimeSpan.FromMinutes(15);
    public const int ForgotPasswordMaxAttempts = 5;

    public static readonly TimeSpan ResetPasswordWindow = TimeSpan.FromMinutes(15);
    public const int ResetPasswordMaxAttempts = 10;
}
