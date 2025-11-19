using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Admin
{
    [Authorize]
    [AdminOnly]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IRoomService _roomService;
        private readonly IUserProfileService _profileService;

        public BookingController(
            IBookingService bookingService,
            IRoomService roomService,
            IUserProfileService profileService)
        {
            _bookingService = bookingService;
            _roomService = roomService;
            _profileService = profileService;
        }

        public IActionResult Index()
        {
            var bookings = _bookingService.GetAllBookings();
            return View(bookings);
        }

        public IActionResult Delete(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
                return NotFound();

            return View(booking);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _bookingService.CancelBooking(id);
            return RedirectToAction("Index");
        }
    }
}
