using Depi_Project.Models;
using Depi_Project.Data.UnitOfWork;
using Depi_Project.Services.Interfaces;

namespace Depi_Project.Services.Implementations
{
    public class RoomTypeService : IRoomTypeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoomTypeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<RoomType> GetAllRoomTypes()
        {
            return _unitOfWork.RoomTypes.GetAll();
        }

        public RoomType GetRoomTypeById(int id)
        {
            return _unitOfWork.RoomTypes.GetById(id);
        }

        public void CreateRoomType(RoomType roomType)
        {
            _unitOfWork.RoomTypes.Add(roomType);
            _unitOfWork.Save();
        }

        public void UpdateRoomType(RoomType roomType)
        {
            _unitOfWork.RoomTypes.Update(roomType);
            _unitOfWork.Save();
        }

        public void DeleteRoomType(int id)
        {
            var roomType = _unitOfWork.RoomTypes.GetById(id);
            if (roomType != null)
            {
                _unitOfWork.RoomTypes.Delete(roomType);
                _unitOfWork.Save();
            }
        }
    }
}
