using Depi_Project.Models;
using Depi_Project.Data.UnitOfWork;
using Depi_Project.Services.Interfaces;

namespace Depi_Project.Services.Implementations
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoomService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<Room> GetAllRooms()
        {
            return _unitOfWork.Rooms.GetAll();
        }

        public Room GetRoomById(int id)
        {
            return _unitOfWork.Rooms.GetById(id);
        }

        public void CreateRoom(Room room)
        {
            _unitOfWork.Rooms.Add(room);
            _unitOfWork.Save();
        }

        public void UpdateRoom(Room room)
        {
            _unitOfWork.Rooms.Update(room);
            _unitOfWork.Save();
        }

        public void DeleteRoom(int id)
        {
            var room = _unitOfWork.Rooms.GetById(id);
            if (room != null)
            {
                _unitOfWork.Rooms.Delete(room);
                _unitOfWork.Save();
            }
        }

        // Example rule: Available = not booked & status "Available"
        public IEnumerable<Room> GetAvailableRooms()
        {
            return _unitOfWork.Rooms
                .GetAll()
                .Where(r => r.Status == "Available");
        }
    }
}
