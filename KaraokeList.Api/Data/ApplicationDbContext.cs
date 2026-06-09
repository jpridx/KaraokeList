using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // DbSets for application tables
        public DbSet<Song> Songs { get; set; } = null!;
        public DbSet<Artist> Artists { get; set; } = null!;
        public DbSet<Genre> Genres { get; set; } = null!;
        public DbSet<Singer> Singers { get; set; } = null!;
        public DbSet<Venue> Venues { get; set; } = null!;
        public DbSet<Performance> Performances { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.SingerId).IsRequired(false);
                entity.HasIndex(u => u.SingerId)
                    .IsUnique()
                    .HasFilter("[SingerId] IS NOT NULL");
            });
        }
    }
}
