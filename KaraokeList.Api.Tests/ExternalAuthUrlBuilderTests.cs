using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class ExternalAuthUrlBuilderTests
{
    [Fact]
    public void BuildStartUrl_includes_optional_query_parameters()
    {
        var url = ExternalAuthUrlBuilder.BuildStartUrl(
            "http://localhost:5299",
            ExternalAuthProviderNames.Google,
            returnUrl: "/log",
            invite: "abc",
            rememberMe: true);

        Assert.Equal(
            "http://localhost:5299/api/auth/external/google?returnUrl=%2Flog&invite=abc&rememberMe=true",
            url);
    }
}
