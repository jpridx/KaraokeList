using Microsoft.AspNetCore.Identity;

namespace KaraokeList.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public int? SingerId { get; set; }
    }

}
