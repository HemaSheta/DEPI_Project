using System.ComponentModel.DataAnnotations;

namespace Depi_Project.Models
{
    public class RoomType
    {
        public int RoomTypeId { get; set; }

        [Required]
        public string RoomTypeName { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public float Price { get; set; }

        [Required]
        public string NumOfPeople { get; set; }

    }
}
