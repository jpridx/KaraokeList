using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class SingerLinkPanelTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();
    private readonly InMemoryLocalStorage localStorage = new();

    public SingerLinkPanelTests()
    {
        this.AddAuthorization();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        AddSyncfusionServices(services);
        services.AddSingleton<IKaraokeApiClient>(api.Object);
        services.AddSingleton<ILocalStorageService>(localStorage);
        services.AddSingleton<ISingerProfileLocalStore>(new SingerProfileLocalStore(localStorage));
        services.AddSingleton<JwtAuthenticationStateProvider>();
    }

    private IRenderedComponent<SingerLinkPanel> RenderPanel(
        Action<ComponentParameterCollectionBuilder<SingerLinkPanel>> configure) =>
        Render<SingerLinkPanel>(configure);

    [Fact]
    public void Renders_nothing_when_not_visible()
    {
        var cut = RenderPanel(parameters => parameters.Add(p => p.Visible, false));

        Assert.Empty(cut.Markup.Trim());
        api.Verify(client => client.GetSingersAsync(), Times.Never);
    }

    [Fact]
    public void Shows_link_panel_when_visible()
    {
        var cut = RenderPanel(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.OnLinked, EventCallback.Factory.Create<int?>(this, _ => { })));

        cut.WaitForAssertion(() =>
            Assert.Contains("Link your account to a singer profile", cut.Markup));
        api.Verify(client => client.GetSingersAsync(), Times.Never);
    }

    [Fact]
    public void Shows_error_when_link_fails()
    {
        api.Setup(client => client.LinkSingerAsync(It.IsAny<LinkSingerRequest>()))
            .ReturnsAsync(AuthResult.Fail("That singer was not found."));

        var cut = RenderPanel(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.OnLinked, EventCallback.Factory.Create<int?>(this, _ => { })));

        cut.WaitForAssertion(() => cut.Find("input.form-control"));
        cut.Find("input.form-control").Change("New Singer");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("That singer was not found.", cut.Markup));
    }

    [Fact]
    public void Invokes_OnLinked_when_link_succeeds()
    {
        api.Setup(client => client.LinkSingerAsync(It.IsAny<LinkSingerRequest>()))
            .ReturnsAsync(AuthResult.Ok(new AuthResponse
            {
                Token = "test-token",
                Email = "singer@example.com",
                SingerId = 42,
                ExpiresUtc = DateTime.UtcNow.AddHours(1)
            }));

        int? linkedSingerId = null;
        var cut = RenderPanel(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.OnLinked, EventCallback.Factory.Create<int?>(this, id => linkedSingerId = id)));

        cut.WaitForAssertion(() => cut.Find("input.form-control"));
        cut.Find("input.form-control").Change("Stage Name");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Equal(42, linkedSingerId));
        api.Verify(client => client.LinkSingerAsync(It.Is<LinkSingerRequest>(r =>
            r.Name == "Stage Name" && r.SingerId == null)), Times.Once);
    }
}
