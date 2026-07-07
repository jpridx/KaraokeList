using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class HostMessagePanelTests : BunitTestContext
{
    public HostMessagePanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_formatted_message_without_key_picker()
    {
        var cut = Render<HostMessagePanel>(parameters => parameters
            .Add(p => p.Title, "Footloose")
            .Add(p => p.ArtistName, "Kenny Loggins")
            .Add(p => p.KeyChangeSemitones, 2)
            .Add(p => p.ShowKeyPicker, false));

        var text = cut.Find(".host-message-text").TextContent;
        Assert.Equal("Footloose - Kenny Loggins (Up 2)", text);
    }

    [Fact]
    public void Disables_copy_button_when_title_is_blank()
    {
        var cut = Render<HostMessagePanel>(parameters => parameters
            .Add(p => p.ShowKeyPicker, false));

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Invokes_clipboard_js_when_copy_clicked()
    {
        JSInterop.SetupVoid("copyTextToClipboard", "Footloose - Kenny Loggins");

        var cut = Render<HostMessagePanel>(parameters => parameters
            .Add(p => p.Title, "Footloose")
            .Add(p => p.ArtistName, "Kenny Loggins")
            .Add(p => p.ShowKeyPicker, false));

        cut.Find("button").Click();

        JSInterop.VerifyInvoke("copyTextToClipboard");
    }

    [Fact]
    public void Renders_co_performer_names_in_message()
    {
        var cut = Render<HostMessagePanel>(parameters => parameters
            .Add(p => p.Title, "Islands in the Stream")
            .Add(p => p.ArtistName, "Kenny Rogers")
            .Add(p => p.ShowKeyPicker, false)
            .Add(p => p.CoPerformerNames, new[] { "Dolly Parton" }));

        var text = cut.Find(".host-message-text").TextContent;
        Assert.Equal("Islands in the Stream - Kenny Rogers (with Dolly Parton)", text);
    }
}
