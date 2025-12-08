using Depi_Project.Models;
using System;
using System.Collections.Generic;

namespace Depi_Project.Services.Interfaces
{
    public interface IBookingService
    {
        IEnumerable<Booking> GetAllBookings();
        Booking GetBookingById(int id);

        // Returns TRUE if booking is created successfully
        bool CreateBooking(Booking booking);

        // Soft-cancel (mark Canceled) — returns true on success
        bool CancelBookingSoft(int id);

        // Hard delete (existing behavior) if needed
        void CancelBooking(int id);

        // For preventing overlapping reservations
        bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut);

        // Validate booking data/constraints before saving
        bool ValidateBooking(Booking booking, out string error);
    }
}
