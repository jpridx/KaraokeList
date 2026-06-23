using KaraokeList.Api.Services;
using KaraokeList.Data;

namespace KaraokeList.Api.IntegrationTests;

public sealed class CapturingAccountEmailSender : IAccountEmailSender
{
    public string? LastEmail { get; private set; }
    public string? LastResetLink { get; private set; }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        LastEmail = email;
        LastResetLink = resetLink;
        return Task.CompletedTask;
    }
}
