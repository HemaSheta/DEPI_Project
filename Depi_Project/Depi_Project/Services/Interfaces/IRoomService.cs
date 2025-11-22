// Services/Interfaces/IRoomService.cs
using Depi_Project.Models;

namespace Depi_Project.Services.Interfaces
{
    public interface IRoomService
    {
        IEnumerable<Room> GetAllRooms();
        Room GetRoomById(int id);
        void CreateRoom(Room room);
        void UpdateRoom(Room room);
        void DeleteRoom(int id);
        IEnumerable<Room> GetAvailableRooms();

        // Filtering for customers (date-based availability)
        IEnumerable<Room> GetRoomsFiltered(
            int? roomType,
            DateTime? checkIn,
            DateTime? checkOut,
            float? minPrice,
            float? maxPrice,
            int? persons,
            string? search);
    }
}
