// Services/Implementations/RoomTypeService.cs
using Depi_Project.Data.UnitOfWork;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using System.Linq;

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
            return _unitOfWork.RoomTypes.GetAll().ToList();
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
            var rt = _unitOfWork.RoomTypes.GetById(id);
            if (rt != null)
            {
                _unitOfWork.RoomTypes.Delete(rt);
                _unitOfWork.Save();
            }
        }

        public bool RoomTypeNameExists(string name, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            var q = _unitOfWork.RoomTypes.GetAll().AsQueryable();

            // normalize (case-insensitive check) - transform to lower
            var normalized = name.Trim().ToLower();

            if (excludeId.HasValue)
            {
                return q.Any(r => r.RoomTypeId != excludeId.Value && r.RoomTypeName.ToLower() == normalized);
            }
            else
            {
                return q.Any(r => r.RoomTypeName.ToLower() == normalized);
            }
        }
    }
}
