using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class AuthResult
{
    public bool Succeeded { get; init; }
    public AuthResponse? Response { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsTransientFailure { get; init; }

    public static AuthResult Ok(AuthResponse response) => new() { Succeeded = true, Response = response };

    public static AuthResult Fail(string message, bool transient = false) =>
        new() { Succeeded = false, ErrorMessage = message, IsTransientFailure = transient };
}
