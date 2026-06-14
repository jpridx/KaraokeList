using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests;

public sealed class KaraokeApiClientTests
{
    private static KaraokeApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

    [Fact]
    public async Task RegisterAsync_ParsesCamelCaseApiErrorMessage()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"message":"Invalid invite code."}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.RegisterAsync(new RegisterRequest
        {
            Name = "Test Singer",
            Email = "user@example.com",
            Password = "TestPassw0rd!23",
            ConfirmPassword = "TestPassw0rd!23"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid invite code.", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WhenUnauthorized_ReturnsFriendlyMessage()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"message":"Invalid login attempt."}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrong"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid login attempt.", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterAsync_WhenSuccessful_ReturnsAuthResponse()
    {
        var expires = DateTime.UtcNow.AddHours(1);
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthResponse
            {
                Token = "jwt-token",
                Email = "user@example.com",
                SingerId = 7,
                ExpiresUtc = expires
            })
        }));

        var result = await client.RegisterAsync(new RegisterRequest
        {
            Name = "Test Singer",
            Email = "user@example.com",
            Password = "TestPassw0rd!23",
            ConfirmPassword = "TestPassw0rd!23"
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal("jwt-token", result.Response.Token);
        Assert.Equal(7, result.Response.SingerId);
    }

    [Fact]
    public async Task GetRegistrationInfoAsync_WhenSuccessful_ReturnsDto()
    {
        var client = CreateClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/auth/registration", request.RequestUri?.PathAndQuery);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RegistrationInfoDto
                {
                    IsRegistrationOpen = true,
                    RequiresInviteCode = true
                })
            };
        }));

        var info = await client.GetRegistrationInfoAsync();

        Assert.NotNull(info);
        Assert.True(info.IsRegistrationOpen);
        Assert.True(info.RequiresInviteCode);
    }

    [Fact]
    public async Task GetRegistrationInfoAsync_WhenApiUnreachable_ReturnsNull()
    {
        var client = CreateClient(new ThrowingHandler());

        var info = await client.GetRegistrationInfoAsync();

        Assert.Null(info);
    }

    [Fact]
    public async Task GetMyRepertoireAsync_ParsesCamelCaseErrorMessage()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"message":"Singer profile required."}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.GetMyRepertoireAsync();

        Assert.False(result.Succeeded);
        Assert.Equal("Singer profile required.", result.ErrorMessage);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("connection refused");
    }
}
