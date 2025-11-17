using System.ComponentModel.DataAnnotations;

namespace Depi_Project.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(11)]
        public int Phone { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
