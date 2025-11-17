using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Depi_Project.Models
{
    public class UserProfile
    {
        public int UserProfileId { get; set; }
        public string IdentityUserId { get; set; }
        public IdentityUser IdentityUser { get; set; }



        public ICollection<Booking> Bookings { get; set; }


    }
}
