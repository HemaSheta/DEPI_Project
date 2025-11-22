// Models/Room.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Depi_Project.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        [Required]
        public int RoomTypeId { get; set; }

        [Required]
        public int RoomNum { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Slide1 { get; set; }

        public string? Slide2 { get; set; }
        public string? Slide3 { get; set; }

        // Navigation
        public RoomType RoomType { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
