using Depi_Project.Data.UnitOfWork;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;

namespace Depi_Project.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IEnumerable<Booking> GetAllBookings()
        {
            return _unitOfWork.Bookings.GetAll();
        }

        public Booking GetBookingById(int id)
        {
            return _unitOfWork.Bookings.GetById(id);
        }

        // Check if room is free during a specific date range
        public bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var bookings = _unitOfWork.Bookings
                .GetAll()
                .Where(b => b.RoomId == roomId);

            foreach (var b in bookings)
            {
                bool overlaps =
                    checkIn < b.CheckOutTime &&
                    checkOut > b.CheckTime;

                if (overlaps)
                    return false;
            }

            return true;
        }

        public bool CreateBooking(Booking booking)
        {
            // ⭐ Prevent user from booking overlapping dates
            var userBookings = _unitOfWork.Bookings
                .GetAll()
                .Where(b => b.IdentityUserId == booking.IdentityUserId);

            foreach (var b in userBookings)
            {
                bool overlaps =
                    booking.CheckTime < b.CheckOutTime &&
                    booking.CheckOutTime > b.CheckTime;

                if (overlaps)
                    return false;
            }

            // ⭐ Check room availability
            if (!IsRoomAvailable(booking.RoomId, booking.CheckTime, booking.CheckOutTime))
                return false;

            // ⭐ Add booking
            _unitOfWork.Bookings.Add(booking);

            // ⭐ Change room status to Booked
            var room = _unitOfWork.Rooms.GetById(booking.RoomId);
            if (room != null)
            {
                room.Status = "Booked";
                _unitOfWork.Rooms.Update(room);
            }

            _unitOfWork.Save();
            return true;
        }

        public void CancelBooking(int id)
        {
            var booking = _unitOfWork.Bookings.GetById(id);

            if (booking != null)
            {
                // Delete booking
                _unitOfWork.Bookings.Delete(booking);

                // Change room back to Available
                var room = _unitOfWork.Rooms.GetById(booking.RoomId);
                if (room != null)
                {
                    room.Status = "Available";
                    _unitOfWork.Rooms.Update(room);
                }

                _unitOfWork.Save();
            }
        }
    }
}
