namespace KaraokeList.Web.Services;

public sealed record ChangePasswordResult(bool Succeeded, string? ErrorMessage)
{
    public static ChangePasswordResult Ok() => new(true, null);
    public static ChangePasswordResult Fail(string message) => new(false, message);
}
