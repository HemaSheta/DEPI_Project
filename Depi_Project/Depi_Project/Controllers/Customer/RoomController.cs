// Controllers/Customer/RoomController.cs
using Depi_Project.Services.Implementations;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Depi_Project.Controllers.Customer
{
    [Route("Customer/Room")]
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IRoomTypeService _roomTypeService;
        private readonly IBookingService _bookingService;

        public RoomController(
            IRoomService roomService,
            IRoomTypeService roomTypeService,
            IBookingService bookingService)
        {
            _roomService = roomService;
            _roomTypeService = roomTypeService;
            _bookingService = bookingService;
        }

        // Matches: /Customer/Room?roomType=1&checkIn=2025-01-01&checkOut=2025-01-05&minPrice=100&maxPrice=500&persons=2&search=...
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index(
            int? roomType,
            string? checkIn,
            string? checkOut,
            float? minPrice,
            float? maxPrice,
            int? persons,
            string? search)
        {
            DateTime? ci = null;
            DateTime? co = null;

            if (!string.IsNullOrWhiteSpace(checkIn) && DateTime.TryParse(checkIn, out var tmpCi))
                ci = tmpCi.Date;

            if (!string.IsNullOrWhiteSpace(checkOut) && DateTime.TryParse(checkOut, out var tmpCo))
                co = tmpCo.Date;

            // If only one of ci/co provided, ignore date filter (or you can treat co = ci+1)
            if ((ci.HasValue && !co.HasValue) || (!ci.HasValue && co.HasValue))
            {
                co = null; // ignore incomplete range
                ci = null;
            }

            // Get filtered rooms (RoomService will filter by bookings if dates provided)
            var rooms = _roomService.GetRoomsFiltered(roomType, ci, co, minPrice, maxPrice, persons, search);

            // Supply room types for the filter select UI
            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            // Pass back filter values so form shows them
            ViewBag.Filter = new
            {
                roomType = roomType,
                checkIn = ci?.ToString("yyyy-MM-dd") ?? "",
                checkOut = co?.ToString("yyyy-MM-dd") ?? "",
                minPrice = minPrice?.ToString(CultureInfo.InvariantCulture) ?? "",
                maxPrice = maxPrice?.ToString(CultureInfo.InvariantCulture) ?? "",
                persons = persons,
                search = search ?? ""
            };

            // Build a set of rooms that are booked today (for "Booked Today" badge)
            var today = DateTime.Today;
            var bookedTodayIds = new HashSet<int>();
            foreach (var r in rooms)
            {
                if (!_bookingService.IsRoomAvailable(r.RoomId, today, today.AddDays(1)))
                    bookedTodayIds.Add(r.RoomId);
            }

            ViewBag.BookedTodayIds = bookedTodayIds;

            return View("~/Views/Customer/Room/Index.cshtml", rooms);
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            // Provide "is booked today" flag for the view
            var today = DateTime.Today;
            var isBookedToday = !_bookingService.IsRoomAvailable(room.RoomId, today, today.AddDays(1));
            ViewBag.IsBookedToday = isBookedToday;

            return View("~/Views/Customer/Room/Details.cshtml", room);
        }
    }
}
