using KaraokeList.Shared;
using Microsoft.AspNetCore.Identity;

namespace KaraokeList.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public int? SingerId { get; set; }
        public Singer? Singer { get; set; }
        public int StaleSongAfterDays { get; set; } = TicklerSettingsLimits.DefaultStaleAfterDays;
        public int StaleSongLimit { get; set; } = TicklerSettingsLimits.DefaultSongLimit;
        public MusicService PreferredMusicService { get; set; } = MusicService.None;
    }

}
