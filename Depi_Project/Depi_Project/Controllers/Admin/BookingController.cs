using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Admin
{
    [Authorize]
    [AdminOnly]
    [Route("Admin/Booking")]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IRoomService _roomService;

        public BookingController(IBookingService bookingService, IRoomService roomService)
        {
            _bookingService = bookingService;
            _roomService = roomService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var bookings = _bookingService.GetAllBookings();
            return View("~/Views/Admin/Booking/Index.cshtml", bookings);
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null) return NotFound();

            return View("~/Views/Admin/Booking/Details.cshtml", booking);
        }

        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null)
                return NotFound();

            return View("~/Views/Admin/Booking/Delete.cshtml", booking);
        }

        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _bookingService.CancelBooking(id);
            return RedirectToAction("Index");
        }
    }
}
