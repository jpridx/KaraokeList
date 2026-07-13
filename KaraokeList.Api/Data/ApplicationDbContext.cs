using KaraokeList.Shared;
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
        public DbSet<PerformanceParticipant> PerformanceParticipants { get; set; } = null!;
        public DbSet<SingerList> SingerLists { get; set; } = null!;
        public DbSet<SingerListSong> SingerListSongs { get; set; } = null!;
        public DbSet<SingerSongTicklerExclusion> SingerSongTicklerExclusions { get; set; } = null!;
        public DbSet<GenreGroup> GenreGroups { get; set; } = null!;
        public DbSet<GenreGroupGenre> GenreGroupGenres { get; set; } = null!;
        public DbSet<SongArtist> SongArtists { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.SingerId).IsRequired(false);
                entity.Property(u => u.StaleSongAfterDays).HasDefaultValue(TicklerSettingsLimits.DefaultStaleAfterDays);
                entity.Property(u => u.StaleSongLimit).HasDefaultValue(TicklerSettingsLimits.DefaultSongLimit);
                entity.Property(u => u.PreferredMusicService).HasDefaultValue(MusicService.None);
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
                entity.Property(a => a.Mbid).HasMaxLength(36);
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

            builder.Entity<PerformanceParticipant>(entity =>
            {
                entity.Property(p => p.DisplayName).HasMaxLength(128);
                entity.HasIndex(p => p.PerformanceId);

                entity.HasOne<Performance>()
                    .WithMany()
                    .HasForeignKey(p => p.PerformanceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Singer>()
                    .WithMany()
                    .HasForeignKey(p => p.SingerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Song>(entity =>
            {
                entity.Property(s => s.RecordingMbid).HasMaxLength(36);
                entity.Property(s => s.ArtistCreditDisplay).HasMaxLength(512);
            });

            builder.Entity<SingerList>(entity =>
            {
                entity.Property(l => l.Kind).HasConversion<int>();
                entity.HasIndex(l => new { l.SingerId, l.Kind }).IsUnique();
                entity.HasOne(l => l.Singer)
                    .WithMany()
                    .HasForeignKey(l => l.SingerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SingerListSong>(entity =>
            {
                entity.HasKey(s => new { s.ListId, s.SongId });
                entity.HasOne(s => s.List)
                    .WithMany(l => l.Songs)
                    .HasForeignKey(s => s.ListId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(s => s.Song)
                    .WithMany()
                    .HasForeignKey(s => s.SongId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SingerSongTicklerExclusion>(entity =>
            {
                entity.HasKey(e => new { e.SingerId, e.SongId });
                entity.Property(e => e.Reason).HasMaxLength(TicklerExclusionValidation.MaxReasonLength);

                entity.HasOne(e => e.Singer)
                    .WithMany()
                    .HasForeignKey(e => e.SingerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Song)
                    .WithMany()
                    .HasForeignKey(e => e.SongId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<GenreGroup>(entity =>
            {
                entity.Property(g => g.GroupName).HasMaxLength(128).IsRequired();
                entity.HasIndex(g => g.GroupName).IsUnique();
            });

            builder.Entity<GenreGroupGenre>(entity =>
            {
                entity.HasKey(m => new { m.GenreGroupId, m.GenreId });

                entity.HasOne(m => m.GenreGroup)
                    .WithMany(g => g.GenreMemberships)
                    .HasForeignKey(m => m.GenreGroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Genre)
                    .WithMany()
                    .HasForeignKey(m => m.GenreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SongArtist>(entity =>
            {
                entity.HasKey(sa => new { sa.SongId, sa.ArtistId });
                entity.HasIndex(sa => sa.ArtistId);

                entity.HasOne(sa => sa.Song)
                    .WithMany()
                    .HasForeignKey(sa => sa.SongId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sa => sa.Artist)
                    .WithMany()
                    .HasForeignKey(sa => sa.ArtistId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
