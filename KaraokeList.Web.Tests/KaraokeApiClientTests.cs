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
    public async Task LoginAsync_WhenTransientFailureThenSuccess_RetriesOnce()
    {
        var attempts = 0;
        var client = CreateClient(new StubHandler(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TaskCanceledException("timeout");
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AuthResponse
                {
                    Token = "jwt-token",
                    Email = "user@example.com",
                    ExpiresUtc = DateTime.UtcNow.AddHours(1)
                })
            };
        }));

        var result = await client.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "TestPassw0rd!23"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task LoginAsync_WhenTransientFailureTwice_ReturnsColdStartMessage()
    {
        var client = CreateClient(new ThrowingHandler());

        var result = await client.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "TestPassw0rd!23"
        });

        Assert.False(result.Succeeded);
        Assert.True(result.IsTransientFailure);
        Assert.Equal(ApiTransientFailure.ColdStartMessage, result.ErrorMessage);
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

    [Fact]
    public async Task GetMyRepertoireAsync_WhenSuccessful_ReturnsParsedSongs()
    {
        var lastPerformed = new DateTime(2025, 6, 1, 20, 0, 0, DateTimeKind.Utc);
        var client = CreateClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/performances/my-repertoire?sortBy=lastPerformed&sortDir=desc", request.RequestUri?.PathAndQuery);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new RepertoireSongDto
                    {
                        SongId = 10,
                        Title = "Footloose",
                        ArtistName = "Kenny Loggins",
                        GenreId = 3,
                        GenreName = "Rock",
                        LastPerformedOn = lastPerformed,
                        PerformanceCount = 2
                    }
                })
            };
        }));

        var result = await client.GetMyRepertoireAsync();

        Assert.True(result.Succeeded);
        Assert.Single(result.Songs);
        Assert.Equal(10, result.Songs[0].SongId);
        Assert.Equal("Footloose", result.Songs[0].Title);
        Assert.Equal(2, result.Songs[0].PerformanceCount);
        Assert.Equal(lastPerformed, result.Songs[0].LastPerformedOn);
    }

    [Fact]
    public async Task GetMyRepertoireAsync_WithFilters_BuildsExpectedQuery()
    {
        string? capturedQuery = null;
        var client = CreateClient(new StubHandler(request =>
        {
            capturedQuery = request.RequestUri?.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<RepertoireSongDto>())
            };
        }));

        var result = await client.GetMyRepertoireAsync(
            sortBy: "title",
            sortDir: "asc",
            genreId: 5,
            includeAll: true);

        Assert.True(result.Succeeded);
        Assert.Equal("/api/performances/my-repertoire?sortBy=title&sortDir=asc&genreId=5&includeAll=true", capturedQuery);
    }

    [Fact]
    public async Task GetMySongSummaryAsync_WhenSuccessful_ReturnsSummary()
    {
        var lastPerformed = new DateTime(2025, 5, 15, 19, 30, 0, DateTimeKind.Utc);
        var client = CreateClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/performances/my-song-summary?songId=42", request.RequestUri?.PathAndQuery);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SongPerformanceSummaryDto
                {
                    SongId = 42,
                    PerformanceCount = 3,
                    LastKeyChangeSemitones = 2,
                    LastPerformedOn = lastPerformed,
                    LastVenueName = "The Stage",
                    History =
                    [
                        new PerformanceHistoryEntryDto
                        {
                            Id = 7,
                            PerformedOn = lastPerformed,
                            VenueId = 3,
                            VenueName = "The Stage",
                            KeyChangeSemitones = 2
                        }
                    ]
                })
            };
        }));

        var result = await client.GetMySongSummaryAsync(42);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Summary);
        Assert.Equal(42, result.Summary.SongId);
        Assert.Equal(3, result.Summary.PerformanceCount);
        Assert.Equal(2, result.Summary.LastKeyChangeSemitones);
        Assert.Equal("The Stage", result.Summary.LastVenueName);
        Assert.Single(result.Summary.History);
    }

    [Fact]
    public async Task GetMySongSummaryAsync_WhenNotFound_ReturnsApiErrorMessage()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"message":"Song not in repertoire."}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.GetMySongSummaryAsync(99);

        Assert.False(result.Succeeded);
        Assert.Equal("Song not in repertoire.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetSongAboutAsync_WhenSuccessful_ReturnsAbout()
    {
        var client = CreateClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/songs/42/about", request.RequestUri?.PathAndQuery);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new SongAboutDto
                {
                    SongId = 42,
                    Title = "Bohemian Rhapsody"
                })
            };
        }));

        var result = await client.GetSongAboutAsync(42);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.About);
        Assert.Equal(42, result.About.SongId);
        Assert.Equal("Bohemian Rhapsody", result.About.Title);
    }

    [Fact]
    public async Task GetSongAboutAsync_WhenNotFound_ReturnsApiErrorMessage()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"message":"Song was not found."}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.GetSongAboutAsync(99);

        Assert.False(result.Succeeded);
        Assert.Equal("Song was not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMyRepertoireGenresAsync_WhenSuccessful_ReturnsGenres()
    {
        var client = CreateClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/performances/my-repertoire/genres", request.RequestUri?.PathAndQuery);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new GenreDto { Id = 1, GenreName = "Rock" },
                    new GenreDto { Id = 2, GenreName = "Pop" }
                })
            };
        }));

        var result = await client.GetMyRepertoireGenresAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Genres.Count);
        Assert.Equal("Rock", result.Genres[0].GenreName);
    }

    [Fact]
    public async Task GetMyRepertoireGenresAsync_WhenUnauthorized_ReturnsErrorMessage()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"message":"Sign in required."}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.GetMyRepertoireGenresAsync();

        Assert.False(result.Succeeded);
        Assert.Equal("Sign in required.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMyListsAsync_WhenNetworkError_ReturnsFriendlyMessage()
    {
        var client = CreateClient(new ThrowingHandler());

        var result = await client.GetMyListsAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Cannot reach the API", result.ErrorMessage);
    }

    [Fact]
    public async Task GetMyPerformancesAsync_WhenServiceUnavailable_ReturnsColdStartMessage()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await client.GetMyPerformancesAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(ApiTransientFailure.ColdStartMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task GetInviteShareAsync_WhenSuccessful_ReturnsInviteCode()
    {
        var client = CreateClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/auth/invite-share", request.RequestUri?.PathAndQuery);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new InviteShareDto
                {
                    CanShare = true,
                    InviteCode = "secret-code"
                })
            };
        }));

        var share = await client.GetInviteShareAsync();

        Assert.NotNull(share);
        Assert.True(share.CanShare);
        Assert.Equal("secret-code", share.InviteCode);
    }

    [Fact]
    public async Task GetInviteShareAsync_WhenApiUnreachable_ReturnsNull()
    {
        var client = CreateClient(new ThrowingHandler());

        var share = await client.GetInviteShareAsync();

        Assert.Null(share);
    }

    [Fact]
    public async Task TryCreatePerformanceAsync_WhenSuccess_ReturnsSucceeded()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)));

        var result = await client.TryCreatePerformanceAsync(new PerformanceDto { Singer = 1, Song = 2, Venue = 3 });

        Assert.True(result.Succeeded);
        Assert.False(result.IsTransient);
    }

    [Fact]
    public async Task TryCreatePerformanceAsync_WhenServiceUnavailable_ReturnsTransientFailure()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await client.TryCreatePerformanceAsync(new PerformanceDto { Singer = 1, Song = 2, Venue = 3 });

        Assert.False(result.Succeeded);
        Assert.True(result.IsTransient);
    }

    [Fact]
    public async Task TryCreatePerformanceAsync_WhenBadRequest_ReturnsPermanentFailure()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"message":"Song is required."}""", Encoding.UTF8, "application/json")
        }));

        var result = await client.TryCreatePerformanceAsync(new PerformanceDto { Singer = 1, Song = 2, Venue = 3 });

        Assert.False(result.Succeeded);
        Assert.False(result.IsTransient);
        Assert.Equal("Song is required.", result.ErrorMessage);
    }

    [Fact]
    public async Task TryCreatePerformanceAsync_WhenNetworkError_ReturnsTransientFailure()
    {
        var client = CreateClient(new ThrowingHandler());

        var result = await client.TryCreatePerformanceAsync(new PerformanceDto { Singer = 1, Song = 2, Venue = 3 });

        Assert.False(result.Succeeded);
        Assert.True(result.IsTransient);
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
