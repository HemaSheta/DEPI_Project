using System.Collections.Generic;

namespace Depi_Project.Models
{
    public class ProfileViewModel
    {
        public UserProfile Profile { get; set; }
        public IEnumerable<Booking> UpcomingBookings { get; set; } = new List<Booking>();
        public IEnumerable<Booking> PastBookings { get; set; } = new List<Booking>();
    }
}
