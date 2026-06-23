using System.Net;
using System.Net.Mail;
using KaraokeList.Data;
using KaraokeList.Security;
using Microsoft.Extensions.Options;

namespace KaraokeList.Api.Services;

public sealed class AccountEmailSender(
    IOptions<EmailSettings> emailOptions,
    ILogger<AccountEmailSender> logger) : IAccountEmailSender
{
    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var settings = emailOptions.Value;
        var subject = "Reset your KaraokeList password";
        var htmlBody =
            $"<p>Reset your password by <a href=\"{WebUtility.HtmlEncode(resetLink)}\">clicking here</a>.</p>" +
            "<p>If you did not request this, you can ignore this email.</p>";

        if (!settings.IsConfigured)
        {
            logger.LogWarning(
                "SMTP is not configured. Password reset link for {Email} (user {UserId}): {ResetLink}",
                email,
                user.Id,
                resetLink);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        await client.SendMailAsync(message);
    }
}
