using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class CoPerformersEditorTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();

    public CoPerformersEditorTests()
    {
        AddSyncfusionServices(Services);
        Services.AddSingleton<IKaraokeApiClient>(api.Object);
        api.Setup(client => client.GetSingersAsync()).ReturnsAsync(
        [
            new SingerDto { Id = 1, Name = "Primary Singer" },
            new SingerDto { Id = 2, Name = "Registered Duet" }
        ]);
    }

    [Fact]
    public void Adding_guest_notifies_parent_with_display_name()
    {
        var cut = RenderComponent<CoPerformerBindingHost>(parameters => parameters
            .Add(p => p.PrimarySingerId, 1));

        cut.Find("input[placeholder='Name not in the app']").Input("Guest Singer");
        ClickGuestAddButton(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.Instance.Performers);
            Assert.Equal("Guest Singer", cut.Instance.Performers[0].DisplayName);
            Assert.Null(cut.Instance.Performers[0].SingerId);
        });
    }

    [Fact]
    public void Adding_guest_updates_host_message_in_parent()
    {
        var cut = RenderComponent<CoPerformerHostMessageHost>(parameters => parameters
            .Add(p => p.PrimarySingerId, 1)
            .Add(p => p.Title, "Islands in the Stream")
            .Add(p => p.ArtistName, "Kenny Rogers"));

        cut.Find("input[placeholder='Name not in the app']").Input("Dolly Parton");
        ClickGuestAddButton(cut);

        cut.WaitForAssertion(() =>
        {
            var message = cut.Find(".host-message-text").TextContent;
            Assert.Equal("Islands in the Stream - Kenny Rogers (with Dolly Parton)", message);
        });
    }

    private static void ClickGuestAddButton(IRenderedFragment cut)
    {
        var addButton = cut.FindAll("button")
            .Last(button => button.TextContent.Trim() == "Add");
        addButton.Click();
    }

    private sealed class CoPerformerBindingHost : ComponentBase
    {
        [Parameter]
        public int PrimarySingerId { get; set; }

        public List<CoPerformerInputDto> Performers { get; private set; } = [];

        private Task OnOtherPerformersChanged(List<CoPerformerInputDto> performers)
        {
            Performers = performers;
            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<CoPerformersEditor>(0);
            builder.AddAttribute(1, nameof(CoPerformersEditor.PrimarySingerId), PrimarySingerId);
            builder.AddAttribute(2, nameof(CoPerformersEditor.OtherPerformers), Performers);
            builder.AddAttribute(3, nameof(CoPerformersEditor.OtherPerformersChanged),
                EventCallback.Factory.Create<List<CoPerformerInputDto>>(this, OnOtherPerformersChanged));
            builder.CloseComponent();
        }
    }

    private sealed class CoPerformerHostMessageHost : ComponentBase
    {
        [Parameter]
        public int PrimarySingerId { get; set; }

        [Parameter]
        public string Title { get; set; } = string.Empty;

        [Parameter]
        public string ArtistName { get; set; } = string.Empty;

        public List<CoPerformerInputDto> Performers { get; private set; } = [];

        private Task OnOtherPerformersChanged(List<CoPerformerInputDto> performers)
        {
            Performers = performers;
            return Task.CompletedTask;
        }

        private IReadOnlyList<string> CoPerformerNames => Performers
            .Select(p => p.DisplayName?.Trim() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<CoPerformersEditor>(0);
            builder.AddAttribute(1, nameof(CoPerformersEditor.PrimarySingerId), PrimarySingerId);
            builder.AddAttribute(2, nameof(CoPerformersEditor.OtherPerformers), Performers);
            builder.AddAttribute(3, nameof(CoPerformersEditor.OtherPerformersChanged),
                EventCallback.Factory.Create<List<CoPerformerInputDto>>(this, OnOtherPerformersChanged));
            builder.CloseComponent();

            builder.OpenComponent<HostMessagePanel>(4);
            builder.AddAttribute(5, nameof(HostMessagePanel.Title), Title);
            builder.AddAttribute(6, nameof(HostMessagePanel.ArtistName), ArtistName);
            builder.AddAttribute(7, nameof(HostMessagePanel.ShowKeyPicker), false);
            builder.AddAttribute(8, nameof(HostMessagePanel.CoPerformerNames), CoPerformerNames);
            builder.CloseComponent();
        }
    }
}
