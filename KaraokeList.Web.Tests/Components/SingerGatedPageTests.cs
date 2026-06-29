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

public sealed class SingerGatedPageTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();

    public SingerGatedPageTests()
    {
        this.AddTestAuthorization();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        AddSyncfusionServices(services);
        services.AddSingleton<IKaraokeApiClient>(api.Object);
        services.AddSingleton<Blazored.LocalStorage.ILocalStorageService>(new InMemoryLocalStorage());
        services.AddSingleton<JwtAuthenticationStateProvider>();
        services.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<JwtAuthenticationStateProvider>());
    }

    [Fact]
    public void Renders_page_shell_and_child_when_singer_resolved()
    {
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 7 });

        var cut = RenderComponent<SingerGatedPage>(parameters => parameters
            .Add(p => p.DocumentTitle, "My Songs")
            .Add(p => p.Title, "My Songs")
            .Add(p => p.WithBottomNav, true)
            .Add(p => p.OnResolved, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.Subtitle, builder => builder.AddMarkupContent(0, "Browse lists"))
            .Add(p => p.ChildContent, (RenderFragment<int>)(singerId => builder =>
                builder.AddMarkupContent(0, $"<p>Content for {singerId}</p>"))));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("My Songs", cut.Markup);
            Assert.Contains("Browse lists", cut.Markup);
            Assert.Contains("Content for 7", cut.Markup);
            Assert.Contains("mobile-page-with-nav", cut.Markup);
        });
    }

    [Fact]
    public void Renders_header_content_above_gate()
    {
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });

        var cut = RenderComponent<SingerGatedPage>(parameters => parameters
            .Add(p => p.DocumentTitle, "Log")
            .Add(p => p.Title, "Log performance")
            .Add(p => p.OnResolved, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.HeaderContent, builder => builder.AddMarkupContent(0, "<p class=\"banner\">Saved!</p>"))
            .Add(p => p.ChildContent, (RenderFragment<int>)(_ => builder =>
                builder.AddMarkupContent(0, "<p>Body</p>"))));

        cut.WaitForAssertion(() => Assert.Contains("Saved!", cut.Markup));
    }

    [Fact]
    public void RequireLinkIfNotLinked_delegates_to_inner_gate()
    {
        api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 9 });

        var cut = RenderComponent<SingerGatedPage>(parameters => parameters
            .Add(p => p.DocumentTitle, "My Songs")
            .Add(p => p.Title, "My Songs")
            .Add(p => p.OnResolved, EventCallback.Factory.Create<int>(this, _ => { }))
            .Add(p => p.ChildContent, (RenderFragment<int>)(singerId => builder =>
                builder.AddMarkupContent(0, $"<p>Singer {singerId}</p>"))));

        cut.WaitForAssertion(() => Assert.Contains("Singer 9", cut.Markup));

        cut.Instance.RequireLinkIfNotLinked("Singer profile is not linked to this account.");
        cut.Render();

        Assert.Contains("Link your account to a singer profile", cut.Markup);
    }
}
