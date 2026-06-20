using System.Net;

namespace KaraokeList.Web.Services;

public static class ApiTransientFailure
{
    public const string ColdStartMessage =
        "The server is still waking up (database cold start). Please wait a moment and try again.";

    public const string ColdStartInProgressMessage =
        "Still waking up the database… this can take up to a minute on first use.";

    public static bool IsTransient(Exception ex) =>
        ex is TaskCanceledException or HttpRequestException or OperationCanceledException;

    public static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
