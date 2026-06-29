using KaraokeList.Shared;
using KaraokeList.Web.Services;
using Moq;

namespace KaraokeList.Web.Tests.Services;

public sealed class InviteShareLoaderTests
{
    [Fact]
    public async Task LoadAsync_when_invite_can_be_shared_builds_urls_and_message()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto { CanShare = true, InviteCode = "abc123" });

        var content = await InviteShareLoader.LoadAsync(api.Object, "https://example.com/app/");

        Assert.True(content.CanShareInvite);
        Assert.Equal("https://example.com/app/register?invite=abc123", content.RegisterUrl);
        Assert.Contains("abc123", content.ShareMessage);
        api.Verify(client => client.GetRegistrationInfoAsync(), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_when_invite_unavailable_loads_registration_info()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetInviteShareAsync())
            .ReturnsAsync(new InviteShareDto
            {
                CanShare = false,
                UnavailableReason = "Registration is closed."
            });
        api.Setup(client => client.GetRegistrationInfoAsync())
            .ReturnsAsync(new RegistrationInfoDto
            {
                IsRegistrationOpen = true,
                RequiresInviteCode = false
            });

        var content = await InviteShareLoader.LoadAsync(api.Object, "https://example.com/");

        Assert.False(content.CanShareInvite);
        Assert.True(content.ShowOpenRegistrationNotice);
        Assert.NotNull(content.RegistrationInfo);
    }
}
