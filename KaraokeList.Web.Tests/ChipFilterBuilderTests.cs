using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Tests;

public sealed class ChipFilterBuilderTests
{
    [Fact]
    public void CreateAllPlusItems_includes_all_chip_and_marks_active_selection()
    {
        var selectedId = 0;
        var chips = ChipFilterBuilder.CreateAllPlusItems(
            activeId: 2,
            items: new[] { new GenreDto { Id = 2, GenreName = "Rock" } },
            getId: genre => genre.Id,
            getLabel: genre => genre.GenreName,
            EventCallback.Factory,
            receiver: new object(),
            onSelectAsync: id =>
            {
                selectedId = id ?? 0;
                return Task.CompletedTask;
            });

        Assert.Equal(2, chips.Count);
        Assert.Equal("All", chips[0].Label);
        Assert.False(chips[0].IsActive);
        Assert.Equal("Rock", chips[1].Label);
        Assert.True(chips[1].IsActive);
    }

    [Fact]
    public void CreateAllPlusItemsByString_includes_all_chip_and_marks_active_selection()
    {
        string? selectedKey = null;
        var chips = ChipFilterBuilder.CreateAllPlusItemsByString(
            activeKey: "Pop",
            items: new[] { "Rock", "Pop" },
            getKey: group => group,
            getLabel: group => group,
            EventCallback.Factory,
            receiver: new object(),
            onSelectAsync: key =>
            {
                selectedKey = key;
                return Task.CompletedTask;
            });

        Assert.Equal(3, chips.Count);
        Assert.Equal("All", chips[0].Label);
        Assert.False(chips[0].IsActive);
        Assert.Equal("Rock", chips[1].Label);
        Assert.False(chips[1].IsActive);
        Assert.Equal("Pop", chips[2].Label);
        Assert.True(chips[2].IsActive);
    }
}
