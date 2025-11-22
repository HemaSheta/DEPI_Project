using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Depi_Project.Models
{
    public class RoomType
    {
        public int RoomTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string RoomTypeName { get; set; }

        [Required]
        public float Price { get; set; }

        [Required]
        public int NumOfPeople { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
