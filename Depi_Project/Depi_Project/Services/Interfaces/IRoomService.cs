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
    }
}
