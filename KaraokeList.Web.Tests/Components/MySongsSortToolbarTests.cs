using Bunit;
using KaraokeList.Web.Components;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class MySongsSortToolbarTests : BunitTestContext
{
    [Fact]
    public void Renders_sort_options_and_genre_toggle_label()
    {
        var cut = RenderComponent<MySongsSortToolbar>(parameters => parameters
            .Add(p => p.SortBy, "title")
            .Add(p => p.SortDir, "desc")
            .Add(p => p.ShowGenreFilters, false)
            .Add(p => p.OnSortChanged, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnToggleSortDir, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnToggleGenreFilters, EventCallback.Factory.Create(this, () => { })));

        Assert.Contains("Song title", cut.Markup);
        Assert.Contains("Show genre filters", cut.Markup);
        Assert.Contains("↓ Newest / Z–A", cut.Markup);
    }

    [Fact]
    public void Invokes_genre_toggle_callback()
    {
        var toggled = false;
        var cut = RenderComponent<MySongsSortToolbar>(parameters => parameters
            .Add(p => p.SortBy, "title")
            .Add(p => p.SortDir, "asc")
            .Add(p => p.ShowGenreFilters, true)
            .Add(p => p.OnSortChanged, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnToggleSortDir, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnToggleGenreFilters, EventCallback.Factory.Create(this, () => toggled = true)));

        cut.FindAll("button")[1].Click();
        Assert.True(toggled);
        Assert.Contains("Hide genre filters", cut.Markup);
    }
}
