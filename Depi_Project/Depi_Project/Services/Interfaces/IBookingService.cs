// Services/Interfaces/IBookingService.cs
using Depi_Project.Models;

namespace Depi_Project.Services.Interfaces
{
    public interface IBookingService
    {
        IEnumerable<Booking> GetAllBookings();
        Booking GetBookingById(int id);

        // Returns TRUE if booking is created successfully
        bool CreateBooking(Booking booking);

        void CancelBooking(int id);

        // For preventing overlapping reservations
        bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut);

        // New: Validate a booking for conflicts and business rules without persisting.
        // Returns true if OK; false + error message otherwise.
        bool ValidateBooking(Booking booking, out string error);
    }
}
