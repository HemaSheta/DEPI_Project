// Services/Implementations/BookingService.cs
using Depi_Project.Data.UnitOfWork;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Depi_Project.Services.Implementations
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Return bookings with navigation properties (Room + RoomType + IdentityUser)
        public IEnumerable<Booking> GetAllBookings()
        {
            var ctx = _unitOfWork.Context;
            return ctx.Bookings
                      .Include(b => b.Room)
                        .ThenInclude(r => r.RoomType)
                      .Include(b => b.IdentityUser)
                      .ToList();
        }

        public Booking GetBookingById(int id)
        {
            var ctx = _unitOfWork.Context;
            return ctx.Bookings
                      .Include(b => b.Room)
                        .ThenInclude(r => r.RoomType)
                      .Include(b => b.IdentityUser)
                      .FirstOrDefault(b => b.BookingId == id);
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

        // Validate booking (no DB changes). Useful to check before persisting.
        public bool ValidateBooking(Booking booking, out string error)
        {
            error = "";

            if (booking == null)
            {
                error = "Booking is null.";
                return false;
            }

            if (booking.CheckOutTime <= booking.CheckTime)
            {
                error = "Check-out must be after check-in.";
                return false;
            }

            if (booking.CheckTime.Date < DateTime.Today)
            {
                error = "Check-in cannot be in the past.";
                return false;
            }

            // 1) Check room availability (against all bookings)
            if (!IsRoomAvailable(booking.RoomId, booking.CheckTime, booking.CheckOutTime))
            {
                error = "Room is not available for the selected dates.";
                return false;
            }

            // 2) If IdentityUserId provided, ensure the user does not already have overlapping booking
            if (!string.IsNullOrWhiteSpace(booking.IdentityUserId))
            {
                var userBookings = _unitOfWork.Bookings
                    .GetAll()
                    .Where(b => b.IdentityUserId == booking.IdentityUserId);

                foreach (var b in userBookings)
                {
                    bool overlaps =
                        booking.CheckTime < b.CheckOutTime &&
                        booking.CheckOutTime > b.CheckTime;

                    if (overlaps)
                    {
                        error = "You already have a booking that overlaps these dates.";
                        return false;
                    }
                }
            }

            // Passed all checks
            return true;
        }

        public bool CreateBooking(Booking booking)
        {
            // Prevent user from booking overlapping dates (their own bookings)
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

            // Check room availability (against all bookings)
            if (!IsRoomAvailable(booking.RoomId, booking.CheckTime, booking.CheckOutTime))
                return false;

            // Add booking
            _unitOfWork.Bookings.Add(booking);

            // IMPORTANT: do NOT change Room.Status here (we keep date-based availability).
            // Availability should be derived from bookings.

            _unitOfWork.Save();
            return true;
        }

        public void CancelBooking(int id)
        {
            var booking = _unitOfWork.Bookings.GetById(id);

            if (booking != null)
            {
                _unitOfWork.Bookings.Delete(booking);
                _unitOfWork.Save();
            }
        }
    }
}
