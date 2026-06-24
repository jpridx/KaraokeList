using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
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

                entity.HasOne(u => u.Singer)
                    .WithMany()
                    .HasForeignKey(u => u.SingerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Artist>(entity =>
            {
                entity.Property(a => a.Name).HasMaxLength(128);
                entity.HasIndex(a => a.Name).IsUnique();
            });

            builder.Entity<Performance>(entity =>
            {
                entity.Property(p => p.Singer).IsRequired();
                entity.Property(p => p.Song).IsRequired();

                entity.HasOne<Singer>()
                    .WithMany()
                    .HasForeignKey(p => p.Singer)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Song>()
                    .WithMany()
                    .HasForeignKey(p => p.Song)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Venue>()
                    .WithMany()
                    .HasForeignKey(p => p.Venue)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Song>(entity =>
            {
                entity.HasOne<Artist>()
                    .WithMany()
                    .HasForeignKey(s => s.Artist)
                    .HasConstraintName("FK_Songs_Artists_Artist")
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Artist>()
                    .WithMany()
                    .HasForeignKey(s => s.SecondaryArtist)
                    .HasConstraintName("FK_Songs_Artists_SecondaryArtist")
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
