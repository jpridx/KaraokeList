using Bunit;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Components;

public sealed class LogSongPickerPanelTests : BunitTestContext
{
    [Fact]
    public void Renders_song_hint_from_parameter_not_literal()
    {
        var cut = RenderComponent<LogSongPickerPanel>(parameters => parameters
            .Add(p => p.Items, Array.Empty<LogSongPickItem>())
            .Add(p => p.SelectedSongId, 1)
            .Add(p => p.SongHint, "You've sung this 3 times.")
            .Add(p => p.UsingOfflineCatalog, true)
            .Add(p => p.WorkingUpSongIds, new HashSet<int>()));

        Assert.Contains("You've sung this 3 times.", cut.Markup);
        Assert.DoesNotContain("songHint", cut.Markup);
    }
}
