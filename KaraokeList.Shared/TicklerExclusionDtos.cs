namespace KaraokeList.Shared;

public class SongTicklerExclusionDto
{
    public bool Excluded { get; set; }
    public string? Reason { get; set; }
}

public class UpdateSongTicklerExclusionRequest
{
    public string? Reason { get; set; }
}

public static class TicklerExclusionValidation
{
    public const int MaxReasonLength = 25;

    public static string? ValidateReason(string? reason)
    {
        if (reason is null)
        {
            return null;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length > MaxReasonLength
            ? $"Reason must be {MaxReasonLength} characters or fewer."
            : null;
    }

    public static string? NormalizeReason(string? reason)
    {
        if (reason is null)
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
