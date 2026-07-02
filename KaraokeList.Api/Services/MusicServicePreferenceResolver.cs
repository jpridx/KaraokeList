using KaraokeList.Data;
using KaraokeList.Shared;

namespace KaraokeList.Api.Services;

public static class MusicServicePreferenceResolver
{
    public static MusicServicePreferenceDto ToDto(ApplicationUser user) => new()
    {
        Service = user.PreferredMusicService
    };

    public static string? Validate(MusicService service) =>
        Enum.IsDefined(service) ? null : "Invalid music service.";
}
