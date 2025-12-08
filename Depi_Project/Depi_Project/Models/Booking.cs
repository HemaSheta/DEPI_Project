using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Depi_Project.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int RoomId { get; set; }

        // Use IdentityUser's Id (string) as the FK
        [Required]
        [ForeignKey(nameof(IdentityUser))] // ensure EF uses this as FK for the IdentityUser navigation
        public string IdentityUserId { get; set; }

        // Navigation
        public virtual IdentityUser IdentityUser { get; set; }

        [Required]
        public DateTime CheckTime { get; set; }

        [Required]
        public DateTime CheckOutTime { get; set; }

        [Required]
        public float TotalPrice { get; set; }

        [Required]
        [RegularExpression("Paid|Pending|Not Paid", ErrorMessage = "PaymentStatus must be ( Paid , Pending , Not Paid )")]
        public string PaymentStatus { get; set; } = "Pending";

        public Room Room { get; set; }
    }
}
