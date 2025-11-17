using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Depi_Project.Models;

namespace Depi_Project.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSets will go here
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relationships will go here

            builder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.UserProfile)
                .WithMany(up => up.Bookings)
                .HasForeignKey(b => b.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<UserProfile>()
                .HasOne(up => up.IdentityUser)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.IdentityUserId)
                .OnDelete(DeleteBehavior.Cascade);


        }
    }
}
