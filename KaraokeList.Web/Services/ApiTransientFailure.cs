using System.Net;

namespace KaraokeList.Web.Services;

public static class ApiTransientFailure
{
    public const string ColdStartMessage =
        "The server is still waking up (database cold start). Please wait a moment and try again.";

    public const string ColdStartInProgressMessage =
        "Still loading… the database may be waking up after idle. This can take up to a minute.";

    public static bool IsTransient(Exception ex) =>
        ex is TaskCanceledException or HttpRequestException or OperationCanceledException;

    public static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
