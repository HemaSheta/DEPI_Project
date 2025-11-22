// Services/Implementations/RoomService.cs
using Depi_Project.Data;
using Depi_Project.Data.UnitOfWork;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Depi_Project.Services.Implementations
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public RoomService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _context = unitOfWork.Context;
        }

        // All rooms with RoomType included
        public IEnumerable<Room> GetAllRooms()
        {
            return _context.Rooms
                .Include(r => r.RoomType)
                .ToList();
        }

        public Room GetRoomById(int id)
        {
            return _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Bookings)
                .FirstOrDefault(r => r.RoomId == id);
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
            var room = _context.Rooms.Find(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                _unitOfWork.Save();
            }
        }

        public IEnumerable<Room> GetAvailableRooms()
        {
            // Here "available" means it is not fully blocked — we keep it simple:
            return _context.Rooms
                .Include(r => r.RoomType)
                .ToList();
        }

        // ======================
        // Filtering with date-based availability
        // ======================
        public IEnumerable<Room> GetRoomsFiltered(
            int? roomType,
            DateTime? checkIn,
            DateTime? checkOut,
            float? minPrice,
            float? maxPrice,
            int? persons,
            string? search)
        {
            // start with rooms and include RoomType + Bookings
            var q = _context.Rooms
                .Include(r => r.RoomType)
                .Include(r => r.Bookings)
                .AsQueryable();

            if (roomType.HasValue)
                q = q.Where(r => r.RoomTypeId == roomType.Value);

            if (minPrice.HasValue)
                q = q.Where(r => r.RoomType != null && r.RoomType.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                q = q.Where(r => r.RoomType != null && r.RoomType.Price <= maxPrice.Value);

            if (persons.HasValue)
                q = q.Where(r => r.RoomType != null && r.RoomType.NumOfPeople == persons.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(r =>
                    r.Description.ToLower().Contains(s) ||
                    (r.RoomType != null && r.RoomType.RoomTypeName.ToLower().Contains(s)) ||
                    r.RoomNum.ToString().Contains(s));
            }

            var list = q.ToList();

            // If user requested date range, remove rooms that have overlapping bookings
            if (checkIn.HasValue && checkOut.HasValue)
            {
                var ci = checkIn.Value.Date;
                var co = checkOut.Value.Date;
                // Keep only rooms where none of the bookings overlap
                list = list.Where(r =>
                    !r.Bookings.Any(b =>
                        // existing booking overlaps requested range
                        ci < b.CheckOutTime && co > b.CheckTime
                    )
                ).ToList();
            }

            return list;
        }
    }
}
