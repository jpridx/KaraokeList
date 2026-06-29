using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Web.Tests.Components;

public sealed class LogSongPickerPanelTests : BunitTestContext
{
    public LogSongPickerPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected override void ConfigureServices(IServiceCollection services) =>
        AddSyncfusionServices(services);

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

    [Fact]
    public async Task Propagates_selected_song_id_to_parent()
    {
        int? parentSongId = null;
        var afterCalled = false;

        var cut = RenderComponent<LogSongPickerPanel>(parameters => parameters
            .Add(p => p.Items,
            [
                new LogSongPickItem(42, "Jeopardy", "The Greg Kihn Band", true, false)
            ])
            .Add(p => p.SelectedSongId, parentSongId)
            .Add(p => p.SelectedSongIdChanged, EventCallback.Factory.Create<int?>(this, value => parentSongId = value))
            .Add(p => p.OnSelectedSongIdAfter, EventCallback.Factory.Create(this, () => afterCalled = true))
            .Add(p => p.UsingOfflineCatalog, true)
            .Add(p => p.WorkingUpSongIds, new HashSet<int>()));

        var picker = cut.FindComponent<CatalogSongPicker>();
        await cut.InvokeAsync(() => picker.Instance.SelectedSongIdChanged.InvokeAsync(42));

        Assert.Equal(42, parentSongId);
        Assert.True(afterCalled);
    }
}
