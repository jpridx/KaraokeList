using System.ComponentModel.DataAnnotations;

namespace KaraokeList.Shared;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
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
    public bool IsAdmin { get; set; }
}

public class AdminUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? SingerId { get; set; }
    public string? SingerName { get; set; }
    public bool IsAdmin { get; set; }
}

public class UpdateAdminUserRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public int? SingerId { get; set; }
}

public class LinkSingerRequest
{
    [StringLength(128, MinimumLength = 1)]
    public string? Name { get; set; }

    public int? SingerId { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ApiErrorResponse
{
    public string? Message { get; set; }
}

public class RegistrationInfoDto
{
    public bool IsRegistrationOpen { get; set; }
    public bool RequiresInviteCode { get; set; }
    public bool IsPasswordRecoveryAllowed { get; set; }
}

public class InviteShareDto
{
    public bool CanShare { get; set; }
    public string? UnavailableReason { get; set; }
    public string? InviteCode { get; set; }
}
