using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class InviteShareContent
{
    public InviteShareDto? Share { get; init; }

    public RegistrationInfoDto? RegistrationInfo { get; init; }

    public string? RegisterUrl { get; init; }

    public string? ShareMessage { get; init; }

    public bool CanShareInvite =>
        Share?.CanShare == true
        && !string.IsNullOrWhiteSpace(Share.InviteCode)
        && !string.IsNullOrWhiteSpace(RegisterUrl);

    public bool ShowOpenRegistrationNotice =>
        !CanShareInvite
        && RegistrationInfo?.IsRegistrationOpen == true
        && RegistrationInfo.RequiresInviteCode == false;
}

public static class InviteShareLoader
{
    public static async Task<InviteShareContent> LoadAsync(IKaraokeApiClient api, string baseUri)
    {
        var share = await api.GetInviteShareAsync();
        if (share?.CanShare == true && !string.IsNullOrWhiteSpace(share.InviteCode))
        {
            var registerUrl = InviteShareFormatting.BuildRegisterUrl(baseUri, share.InviteCode);
            return new InviteShareContent
            {
                Share = share,
                RegisterUrl = registerUrl,
                ShareMessage = InviteShareFormatting.BuildShareMessage(registerUrl, share.InviteCode)
            };
        }

        return new InviteShareContent
        {
            Share = share,
            RegistrationInfo = await api.GetRegistrationInfoAsync()
        };
    }
}
