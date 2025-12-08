// Data/AppDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Depi_Project.Models;

namespace Depi_Project.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // RoomType -> Rooms (1:N)
            builder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking -> Room (N:1)
            builder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking -> IdentityUser (N:1)
            // Explicit navigation mapping to avoid EF creating duplicate FK columns.
            builder.Entity<Booking>()
                .HasOne(b => b.IdentityUser)
                .WithMany() // we don't expose a collection on IdentityUser
                .HasForeignKey(b => b.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserProfile -> IdentityUser
            builder.Entity<UserProfile>()
                .HasOne(up => up.IdentityUser)
                .WithMany()
                .HasForeignKey(up => up.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
