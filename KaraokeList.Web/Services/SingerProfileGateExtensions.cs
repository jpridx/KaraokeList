using KaraokeList.Web.Components;

namespace KaraokeList.Web.Services;

public static class SingerProfileGateExtensions
{
    public static void RequireLinkIfNotLinked(this SingerProfileGate? gate, string? errorMessage)
    {
        if (errorMessage?.Contains("not linked", StringComparison.OrdinalIgnoreCase) == true)
        {
            gate?.RequireSingerLink();
        }
    }

    public static void RequireLinkIfNotLinked(this SingerGatedPage? page, string? errorMessage) =>
        page?.Gate.RequireLinkIfNotLinked(errorMessage);
}
