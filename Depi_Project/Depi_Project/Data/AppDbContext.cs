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

            // Booking -> IdentityUser (many bookings can belong to one Identity user)
            builder.Entity<Booking>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(b => b.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserProfile -> IdentityUser
            // Explicitly map UserProfile.IdentityUserId -> AspNetUsers.Id with Restrict delete behavior.
            builder.Entity<UserProfile>()
                .HasOne(up => up.IdentityUser)
                .WithMany() // user can have many dependent entities, but we don't expose navigation from IdentityUser
                .HasForeignKey(up => up.IdentityUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
