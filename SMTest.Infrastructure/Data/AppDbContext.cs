using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMTest.Domain.Entities;
using SMTest.Domain.ValueObjects;

namespace SMTest.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public DbSet<ShortUrl> ShortUrls { get; set; }
        public DbSet<RateLimitRecord> RateLimitRecords { get; set; }
        public DbSet<DailyLimit> UserDailyLimits { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShortUrl>(entity =>
            {
                entity.ToTable("ShortUrls");

                entity.HasKey(s => s.ShortCode);

                entity.Property(s => s.ShortCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(s => s.LongUrl)
                    .IsRequired()
                    .HasMaxLength(2048);

                entity.Property(s => s.HitCount)
                    .HasDefaultValue(0);

                entity.Property(s => s.UserId)
                    .IsRequired();

                entity.HasOne(s => s.User)
                    .WithMany(u => u.ShortUrls)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);  // Optional: decide on delete behavior

                entity.Property(s => s.RowVersion)
                    .IsRowVersion();
            });

            modelBuilder.Entity<RateLimitRecord>()
                .HasIndex(r => new { r.ShortCode, r.Timestamp });

            modelBuilder.Entity<DailyLimit>()
                .HasIndex(u => u.UserId)
                .IsUnique();
        }
    }

}
