using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor.Grids;

namespace KaraokeList.Web.Tests.Components;

public sealed class CatalogCrudGridTests : BunitTestContext
{
    public CatalogCrudGridTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        AddSyncfusionServices(Services);
    }

    [Fact]
    public void Shows_database_wakeup_message_while_loading()
    {
        var cut = RenderCatalogGrid(isLoading: true);

        Assert.Contains(ApiTransientFailure.DatabaseWakingUpMessage, cut.Markup);
        Assert.Contains("spinner-border", cut.Markup);
        Assert.DoesNotContain("e-grid", cut.Markup);
    }

    [Fact]
    public void Renders_grid_when_not_loading()
    {
        var cut = RenderCatalogGrid(
            isLoading: false,
            items: [new VenueDto { Id = 1, VenueName = "Main Stage" }]);

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(ApiTransientFailure.DatabaseWakingUpMessage, cut.Markup);
            Assert.Contains("Main Stage", cut.Markup);
        });
    }

    [Fact]
    public async Task SyncEditingRowAsync_does_not_throw_when_grid_not_rendered()
    {
        var cut = RenderCatalogGrid(isLoading: true);
        var item = new VenueDto { Id = 1, VenueName = "Main Stage" };

        var exception = await Record.ExceptionAsync(() => cut.Instance.SyncEditingRowAsync(item));

        Assert.Null(exception);
    }

    private IRenderedComponent<CatalogCrudGrid<VenueDto>> RenderCatalogGrid(
        bool isLoading,
        IReadOnlyList<VenueDto>? items = null) =>
        Render<CatalogCrudGrid<VenueDto>>(parameters => parameters
            .Add(p => p.IsLoading, isLoading)
            .Add(p => p.Items, items ?? [])
            .Add(p => p.GetId, venue => venue.Id)
            .Add(p => p.OnSaveAsync, (_, _) => Task.FromResult(CatalogMutateResult.Ok()))
            .Add(p => p.OnDeleteAsync, _ => Task.FromResult(CatalogMutateResult.Ok()))
            .Add(p => p.OnReload, () => Task.CompletedTask)
            .Add(p => p.Columns, builder =>
            {
                builder.OpenComponent(0, typeof(GridColumn));
                builder.AddAttribute(1, nameof(GridColumn.Field), nameof(VenueDto.VenueName));
                builder.AddAttribute(2, nameof(GridColumn.HeaderText), "Venue Name");
                builder.CloseComponent();
            }));
}
