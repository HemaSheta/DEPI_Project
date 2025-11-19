using Depi_Project.Services.Interfaces;
using Depi_Project.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Depi_Project.Controllers.Customer
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IRoomService _roomService;
        private readonly UserManager<IdentityUser> _userManager;

        public BookingController(
            IBookingService bookingService,
            IRoomService roomService,
            UserManager<IdentityUser> userManager)
        {
            _bookingService = bookingService;
            _roomService = roomService;
            _userManager = userManager;
        }

        // GET: /Customer/Booking/Create?roomId=5
        public IActionResult Create(int roomId)
        {
            var room = _roomService.GetRoomById(roomId);
            if (room == null) return NotFound();

            ViewBag.Room = room;
            return View();
        }

        // POST: Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Booking booking)
        {
            if (ModelState.IsValid)
            {
                // Get logged-in user (IdentityUser)
                string identityUserId = _userManager.GetUserId(User);

                // Attach the logged-in user's ID to the booking
                booking.IdentityUserId = identityUserId;

                // Create booking
                bool created = _bookingService.CreateBooking(booking);

                if (!created)
                {
                    TempData["Error"] = "Sorry, this room is not available for these dates.";
                    return RedirectToAction("Create", new { roomId = booking.RoomId });
                }

                return RedirectToAction("Success");
            }

            return View(booking);
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}
