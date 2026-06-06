using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class AuthResult
{
    public bool Succeeded { get; init; }
    public AuthResponse? Response { get; init; }
    public string? ErrorMessage { get; init; }

    public static AuthResult Ok(AuthResponse response) => new() { Succeeded = true, Response = response };

    public static AuthResult Fail(string message) => new() { Succeeded = false, ErrorMessage = message };
}
