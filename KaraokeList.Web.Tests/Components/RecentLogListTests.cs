using Bunit;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class RecentLogListTests : BunitTestContext
{
    private static readonly RecentLoggedPerformance SampleEntry = new(
        SongId: 42,
        Title: "Jeopardy",
        ArtistName: "The Greg Kihn Band",
        VenueName: "Main Stage",
        PerformedOn: new DateTime(2026, 6, 15),
        KeyChangeSemitones: 2,
        LoggedAt: DateTime.UtcNow);

    [Fact]
    public void Renders_nothing_when_items_empty()
    {
        var cut = Render<RecentLogList>(parameters => parameters
            .Add(p => p.Items, Array.Empty<RecentLoggedPerformance>())
            .Add(p => p.Heading, "Recently logged"));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Link_mode_renders_anchor_with_song_id()
    {
        var cut = Render<RecentLogList>(parameters => parameters
            .Add(p => p.Items, new[] { SampleEntry })
            .Add(p => p.Heading, "Recently logged")
            .Add(p => p.UseLinks, true)
            .Add(p => p.IncludeKeyInMeta, true));

        Assert.Contains("Recently logged", cut.Markup);
        Assert.Contains("href=\"log?songId=42\"", cut.Markup);
        Assert.Contains("Main Stage", cut.Markup);
        Assert.Contains(SampleEntry.PerformedOn.ToString("d"), cut.Markup);
    }

    [Fact]
    public void Button_mode_invokes_selection_callback()
    {
        var selectedId = 0;
        var cut = Render<RecentLogList>(parameters => parameters
            .Add(p => p.Items, new[] { SampleEntry })
            .Add(p => p.OnItemSelected, EventCallback.Factory.Create<int>(this, id => selectedId = id)));

        cut.Find("button.recent-log-item").Click();

        Assert.Equal(42, selectedId);
    }

    [Fact]
    public void MaxItems_limits_visible_rows()
    {
        var items = Enumerable.Range(1, 5)
            .Select(i => SampleEntry with { SongId = i })
            .ToList();

        var cut = Render<RecentLogList>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.MaxItems, 2));

        Assert.Equal(2, cut.FindAll("li").Count);
    }
}
