using System;
using System.ComponentModel.DataAnnotations;
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
        public string IdentityUserId { get; set; }

        // Navigation
        public virtual Microsoft.AspNetCore.Identity.IdentityUser IdentityUser { get; set; }

        [Required]
        public DateTime CheckTime { get; set; }

        [Required]
        public DateTime CheckOutTime { get; set; }

        [Required]
        public float TotalPrice { get; set; }

        [Required]
        [RegularExpression("Paid|Pending", ErrorMessage = "PaymentStatus must be ( Paid , Pending )")]
        public string PaymentStatus { get; set; } = "Pending";

        public Room Room { get; set; }
    }
}
