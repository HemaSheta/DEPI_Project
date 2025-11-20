using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Depi_Project.Models
{
    public class UserProfile
    {
        public int UserProfileId { get; set; }

        [Required]
        [MaxLength(450)]
        public string IdentityUserId { get; set; }

        // Mark navigation nullable to avoid EF conventions creating a shadow FK
        [ForeignKey(nameof(IdentityUserId))]
        public IdentityUser? IdentityUser { get; set; }

        // Optional User Info (you can display/edit these)
        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [MaxLength(150)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        // Navigation
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
