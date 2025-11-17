using System.ComponentModel.DataAnnotations;

namespace Depi_Project.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime CheckTime { get; set; }

        [Required]
        public DateTime CheckOutTime { get; set; }

        [Required]
        public float TotalPrice { get; set; }

        [Required]
        [RegularExpression("Paid|Pending", ErrorMessage = "PaymentStatus must be ( Paid , Pending )")]
        public string PaymentStatus { get; set; }
    }
}
