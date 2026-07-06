using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class TicklerExclusionPanelTests : AuthPageTestContext
{
    [Fact]
    public void Shows_exclude_form_when_song_is_not_excluded()
    {
        Api.Setup(client => client.GetSongTicklerExclusionAsync(42))
            .ReturnsAsync(SongTicklerExclusionResult.Ok(new SongTicklerExclusionDto { Excluded = false }));

        var cut = Render<TicklerExclusionPanel>(parameters => parameters
            .Add(p => p.SongId, 42));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Exclude from tickler", cut.Markup);
            Assert.Contains("Reason (optional, 25 characters)", cut.Markup);
        });
    }

    [Fact]
    public void Shows_excluded_state_with_reason()
    {
        Api.Setup(client => client.GetSongTicklerExclusionAsync(7))
            .ReturnsAsync(SongTicklerExclusionResult.Ok(new SongTicklerExclusionDto
            {
                Excluded = true,
                Reason = "too hard"
            }));

        var cut = Render<TicklerExclusionPanel>(parameters => parameters
            .Add(p => p.SongId, 7));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Excluded from tickler suggestions.", cut.Markup);
            Assert.Contains("Reason: too hard", cut.Markup);
            Assert.Contains("Include in tickler again", cut.Markup);
        });
    }
}
