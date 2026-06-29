using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class StatusAlertsTests : BunitTestContext
{
    [Fact]
    public void Renders_success_alert()
    {
        var cut = RenderComponent<StatusAlerts>(parameters => parameters
            .Add(p => p.SuccessMessage, "Saved."));

        Assert.Contains("alert-success", cut.Markup);
        Assert.Contains("Saved.", cut.Markup);
        Assert.Contains("role=\"status\"", cut.Markup);
    }

    [Fact]
    public void Renders_error_alert()
    {
        var cut = RenderComponent<StatusAlerts>(parameters => parameters
            .Add(p => p.ErrorMessage, "Could not load."));

        Assert.Contains("alert-danger", cut.Markup);
        Assert.Contains("Could not load.", cut.Markup);
        Assert.Contains("role=\"alert\"", cut.Markup);
    }

    [Fact]
    public void Renders_inline_error_when_requested()
    {
        var cut = RenderComponent<StatusAlerts>(parameters => parameters
            .Add(p => p.ErrorMessage, "Network error")
            .Add(p => p.InlineError, true)
            .Add(p => p.Small, true));

        Assert.Contains("text-danger", cut.Markup);
        Assert.Contains("Network error", cut.Markup);
        Assert.DoesNotContain("alert-danger", cut.Markup);
    }

    [Fact]
    public void Renders_warning_alert()
    {
        var cut = RenderComponent<StatusAlerts>(parameters => parameters
            .Add(p => p.WarningMessage, "Using cached data."));

        Assert.Contains("alert-warning", cut.Markup);
        Assert.Contains("Using cached data.", cut.Markup);
    }

    [Fact]
    public void Renders_nothing_when_all_messages_empty()
    {
        var cut = RenderComponent<StatusAlerts>();

        Assert.Empty(cut.Markup.Trim());
    }
}
