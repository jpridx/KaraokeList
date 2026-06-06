using System.ComponentModel.DataAnnotations;

namespace KaraokeList.Shared;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required, StringLength(128, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? InviteCode { get; set; }

    public string? Website { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? SingerId { get; set; }
    public DateTime ExpiresUtc { get; set; }
}

public class UserProfileDto
{
    public string Email { get; set; } = string.Empty;
    public int? SingerId { get; set; }
}

public class LinkSingerRequest
{
    [StringLength(128, MinimumLength = 1)]
    public string? Name { get; set; }

    public int? SingerId { get; set; }
}

public class ApiErrorResponse
{
    public string? Message { get; set; }
}
