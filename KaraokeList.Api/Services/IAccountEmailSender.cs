using KaraokeList.Data;

namespace KaraokeList.Api.Services;

public interface IAccountEmailSender
{
    Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink);
}
