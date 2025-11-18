using Depi_Project.Models;

namespace Depi_Project.Services.Interfaces
{
    public interface IRoomTypeService
    {
        IEnumerable<RoomType> GetAllRoomTypes();
        RoomType GetRoomTypeById(int id);
        void CreateRoomType(RoomType roomType);
        void UpdateRoomType(RoomType roomType);
        void DeleteRoomType(int id);
    }
}
