using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Depi_Project.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        public int RoomTypeId { get; set; }

        [Required]
        [RegularExpression("Available|Full|Booked", ErrorMessage = "Status must be Available, Full, Booked")]
        public string Status { get; set; }

        [Required]
        public int RoomNum { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Slide1 { get; set; }
        [Required]
        public string Slide2 { get; set; }
        [Required]
        public string Slide3 { get; set; }


        public RoomType RoomType { get; set; }
        public ICollection<Booking> Bookings { get; set; }

    }
}
