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
    }
}
