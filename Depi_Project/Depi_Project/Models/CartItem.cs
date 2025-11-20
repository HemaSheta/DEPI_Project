using System;

namespace Depi_Project.Models
{
    public class CartItem
    {
        public int RoomId { get; set; }
        public int RoomNum { get; set; }
        public string RoomTypeName { get; set; }
        public float PricePerNight { get; set; }
        public string Slide1 { get; set; }

        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public int Nights => (int)Math.Ceiling((CheckOut - CheckIn).TotalDays);
        public float Total => Nights * PricePerNight;
    }
}
