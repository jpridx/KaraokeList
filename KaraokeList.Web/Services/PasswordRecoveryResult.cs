namespace KaraokeList.Web.Services;

public sealed record PasswordRecoveryResult(bool Succeeded, string? ErrorMessage)
{
    public static PasswordRecoveryResult Ok() => new(true, null);
    public static PasswordRecoveryResult Fail(string message) => new(false, message);
}
