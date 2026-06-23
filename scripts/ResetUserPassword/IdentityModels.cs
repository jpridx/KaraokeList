using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ResetUserPassword;

public sealed class ApplicationUser : IdentityUser
{
    public int? SingerId { get; set; }
}

public sealed class ResetDbContext(DbContextOptions<ResetDbContext> options)
    : IdentityDbContext<ApplicationUser>(options);
