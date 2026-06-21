using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class InviteShareFormattingTests
{
    [Fact]
    public void BuildRegisterUrl_EncodesInviteCodeAndTrimsBaseUri()
    {
        var url = InviteShareFormatting.BuildRegisterUrl("https://karaoke.example.net/", "abc+def/ghi");

        Assert.Equal("https://karaoke.example.net/register?invite=abc%2Bdef%2Fghi", url);
    }

    [Fact]
    public void BuildShareMessage_IncludesLinkAndInviteCode()
    {
        const string registerUrl = "https://karaoke.example.net/register?invite=secret";
        var message = InviteShareFormatting.BuildShareMessage(registerUrl, "secret");

        Assert.Contains("KaraokeList", message);
        Assert.Contains(registerUrl, message);
        Assert.Contains("Invite code: secret", message);
    }
}
