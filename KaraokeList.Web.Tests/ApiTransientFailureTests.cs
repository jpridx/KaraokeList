using System.Net;
using KaraokeList.Shared;

namespace KaraokeList.Web.Tests;

public sealed class ApiTransientFailureTests
{
    [Theory]
    [InlineData(typeof(TaskCanceledException), true)]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(OperationCanceledException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    public void IsTransient_exception_matches_expected(Type exceptionType, bool expected)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Equal(expected, ApiTransientFailure.IsTransient(ex));
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public void IsTransient_status_code_matches_expected(HttpStatusCode statusCode, bool expected)
    {
        Assert.Equal(expected, ApiTransientFailure.IsTransient(statusCode));
    }
}
