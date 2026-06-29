using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class MySongsGroupedListTests : BunitTestContext
{
    [Fact]
    public void Renders_group_headings_and_load_more()
    {
        var songs = new List<RepertoireSongDto>
        {
            new() { SongId = 1, Title = "Alpha", ArtistName = "Artist", GenreName = "Rock" },
            new() { SongId = 2, Title = "Beta", ArtistName = "Artist", GenreName = "Pop" }
        };
        var paging = new GroupedPagingState();
        var view = paging.BuildVisible(songs);

        var cut = RenderComponent<MySongsGroupedList>(parameters => parameters
            .Add(p => p.PagingView, view)
            .Add(p => p.TotalCount, songs.Count)
            .Add(p => p.OnLoadMore, EventCallback.Factory.Create(this, () => { }))
            .Add(p => p.OnHistory, EventCallback.Factory.Create<RepertoireSongDto>(this, _ => { }))
            .Add(p => p.OnLog, EventCallback.Factory.Create<RepertoireSongDto>(this, _ => { })));

        Assert.Contains("Rock", cut.Markup);
        Assert.Contains("Pop", cut.Markup);
        Assert.Contains("Alpha", cut.Markup);
        Assert.Contains("Beta", cut.Markup);
    }
}
