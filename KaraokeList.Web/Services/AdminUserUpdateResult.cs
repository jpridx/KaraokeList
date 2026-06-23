namespace KaraokeList.Web.Services;

public sealed record AdminUserUpdateResult(bool Succeeded, string? ErrorMessage)
{
    public static AdminUserUpdateResult Ok() => new(true, null);
    public static AdminUserUpdateResult Fail(string message) => new(false, message);
}
