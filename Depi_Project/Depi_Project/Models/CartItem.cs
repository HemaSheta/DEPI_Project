using System;

namespace Depi_Project.Models
{
    // A light-weight DTO stored in session for the reservation cart
    public class CartItem
    {
        public int RoomId { get; set; }
        public int RoomNum { get; set; }
        public string RoomTitle { get; set; } = "";
        public string Slide { get; set; } = "";
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public float PricePerNight { get; set; }
        public float TotalPrice { get; set; }
    }
}
