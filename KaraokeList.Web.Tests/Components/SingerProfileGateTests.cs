using System.Security.Claims;
using Blazored.LocalStorage;
using Bunit;
using Bunit.TestDoubles;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class SingerProfileGateTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();
    private readonly InMemoryLocalStorage localStorage = new();

    public SingerProfileGateTests()
    {
        this.AddAuthorization();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        AddSyncfusionServices(services);
        services.AddSingleton<IKaraokeApiClient>(api.Object);
        services.AddSingleton<ILocalStorageService>(localStorage);
        services.AddSingleton<JwtAuthenticationStateProvider>();
        services.AddSingleton<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());
    }

    [Fact]
    public async Task Shows_link_panel_when_singer_id_missing()
    {
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync((UserProfileDto?)null);

        var cut = Render<SingerProfileGate>(parameters => parameters
            .Add(p => p.OnResolved, EventCallback.Factory.Create<int>(this, _ => { })));

        cut.WaitForAssertion(() =>
            Assert.Contains("Link your account to a singer profile", cut.Markup));
    }

    [Fact]
    public async Task Renders_child_content_and_invokes_OnResolved_when_singer_known()
    {
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 9 });

        var resolvedId = 0;
        var cut = Render<SingerProfileGate>(parameters => parameters
            .Add(p => p.OnResolved, EventCallback.Factory.Create<int>(this, id => resolvedId = id))
            .Add(p => p.ChildContent, (RenderFragment<int>)(singerId => builder =>
                builder.AddMarkupContent(0, $"<p>Singer {singerId}</p>"))));

        cut.WaitForAssertion(() => Assert.Contains("Singer 9", cut.Markup));
        Assert.Equal(9, resolvedId);
    }

    [Fact]
    public async Task Invokes_OnResolved_after_successful_link()
    {
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync((UserProfileDto?)null);
        api.Setup(client => client.LinkSingerAsync(It.IsAny<LinkSingerRequest>()))
            .ReturnsAsync(AuthResult.Ok(new AuthResponse
            {
                Token = "test-token",
                Email = "singer@example.com",
                SingerId = 42,
                ExpiresUtc = DateTime.UtcNow.AddHours(1)
            }));

        var resolvedIds = new List<int>();
        var cut = Render<SingerProfileGate>(parameters => parameters
            .Add(p => p.OnResolved, EventCallback.Factory.Create<int>(this, id => resolvedIds.Add(id)))
            .Add(p => p.ChildContent, (RenderFragment<int>)(singerId => builder =>
                builder.AddMarkupContent(0, $"<p>Singer {singerId}</p>"))));

        cut.WaitForAssertion(() => cut.Find("input.form-control"));
        cut.Find("input.form-control").Change("Stage Name");
        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Singer 42", cut.Markup));
        Assert.Equal([42], resolvedIds);
    }

    [Fact]
    public async Task RequireSingerLink_shows_link_panel_after_child_was_rendered()
    {
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 9 });

        var cut = Render<SingerProfileGate>(parameters => parameters
            .Add(p => p.OnResolved, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.ChildContent, (RenderFragment<int>)(singerId => builder =>
                builder.AddMarkupContent(0, $"<p>Singer {singerId}</p>"))));

        cut.WaitForAssertion(() => Assert.Contains("Singer 9", cut.Markup));

        cut.Instance.RequireSingerLink();
        cut.Render();

        Assert.Contains("Link your account to a singer profile", cut.Markup);
        Assert.DoesNotContain("Singer 9", cut.Markup);
    }
}
