namespace KaraokeList.Web.Services;

public sealed record PerformanceCreateResult(bool Succeeded, bool IsTransient, string? ErrorMessage);
