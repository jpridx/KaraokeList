namespace KaraokeList.Web.Services;

public sealed record CatalogMutateResult(bool Succeeded, string? ErrorMessage)
{
    public static CatalogMutateResult Ok() => new(true, null);

    public static CatalogMutateResult Fail(string message) => new(false, message);
}
