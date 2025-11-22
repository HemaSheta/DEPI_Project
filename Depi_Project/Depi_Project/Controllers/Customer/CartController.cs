using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Depi_Project.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Customer
{
    [Route("Customer/[controller]")]
    public class CartController : Controller
    {
        private const string SESSION_CART_KEY = "reservation_cart_v1";
        private readonly IRoomService _roomService;
        private readonly IBookingService _bookingService;

        public CartController(IRoomService roomService, IBookingService bookingService)
        {
            _roomService = roomService;
            _bookingService = bookingService;
        }

        // GET: /Customer/Cart
        [HttpGet("")]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(SESSION_CART_KEY) ?? new List<CartItem>();
            ViewBag.Total = cart.Sum(i => i.TotalPrice);
            return View("~/Views/Customer/Cart/Index.cshtml", cart);
        }

        // GET: /Customer/Cart/Count
        [HttpGet("Count")]
        public IActionResult Count()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(SESSION_CART_KEY) ?? new List<CartItem>();
            return Json(new { count = cart.Count });
        }

        // GET: /Customer/Cart/Add/{id}?checkIn=yyyy-MM-dd&checkOut=yyyy-MM-dd&stay=true
        [HttpGet("Add/{id}")]
        public IActionResult Add(int id, string? checkIn, string? checkOut, bool stay = false)
        {
            // Require dates
            if (string.IsNullOrEmpty(checkIn) || string.IsNullOrEmpty(checkOut))
            {
                TempData["Error"] = "Please provide check-in and check-out dates.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Customer/Room");
            }

            if (!DateTime.TryParse(checkIn, out var ci) || !DateTime.TryParse(checkOut, out var co))
            {
                TempData["Error"] = "Invalid dates format.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Customer/Room");
            }

            if (co <= ci)
            {
                TempData["Error"] = "Check-out must be after check-in.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Customer/Room");
            }

            var room = _roomService.GetRoomById(id);
            if (room == null)
            {
                return NotFound();
            }

            // Check availability using booking service
            if (!_bookingService.IsRoomAvailable(room.RoomId, ci, co))
            {
                TempData["Error"] = "Room is not available for the selected dates.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Customer/Room");
            }

            // Compute nights
            var nights = (co.Date - ci.Date).Days;
            if (nights <= 0) nights = 1;

            var pricePerNight = room.RoomType?.Price ?? 0f;
            var total = pricePerNight * nights;

            var cart = HttpContext.Session.GetObject<List<CartItem>>(SESSION_CART_KEY) ?? new List<CartItem>();

            // Prevent duplicate same room for same dates
            bool duplicate = cart.Any(c =>
                c.RoomId == room.RoomId
                && c.CheckIn.Date == ci.Date
                && c.CheckOut.Date == co.Date);

            if (duplicate)
            {
                TempData["Error"] = "This room/date is already in your cart.";
                if (stay) return Redirect("/Customer/Room");
                return Redirect("/Customer/Cart");
            }

            // NEW: Prevent overlapping cart items across different rooms (customer rule)
            bool overlapsWithCart = cart.Any(c =>
                ci < c.CheckOut && co > c.CheckIn); // overlap test
            if (overlapsWithCart)
            {
                TempData["Error"] = "You already have a reservation in your cart that overlaps these dates. You cannot book multiple rooms for overlapping dates.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Customer/Room");
            }

            var item = new CartItem
            {
                RoomId = room.RoomId,
                RoomNum = room.RoomNum,
                RoomTitle = room.RoomType?.RoomTypeName ?? $"Room {room.RoomNum}",
                Slide = string.IsNullOrEmpty(room.Slide1) ? "" : room.Slide1,
                CheckIn = ci,
                CheckOut = co,
                PricePerNight = pricePerNight,
                TotalPrice = total
            };

            cart.Add(item);
            HttpContext.Session.SetObject(SESSION_CART_KEY, cart);

            // Redirect behavior:
            if (stay)
            {
                TempData["Success"] = "Added to cart.";
                return Redirect("/Customer/Room");
            }

            TempData["Success"] = "Added to cart.";
            return Redirect("/Customer/Cart");
        }


        [HttpGet("Success")]
        public IActionResult Success()
        {
            HttpContext.Session.Remove("reservation_cart_v1");  // clear cart
            return View("~/Views/Customer/Cart/Success.cshtml");
        }


        // POST: /Customer/Cart/Remove
        [HttpPost("Remove")]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int roomId, string checkIn, string checkOut)
        {
            if (!DateTime.TryParse(checkIn, out var ci) || !DateTime.TryParse(checkOut, out var co))
                return Redirect("/Customer/Cart");

            var cart = HttpContext.Session.GetObject<List<CartItem>>(SESSION_CART_KEY) ?? new List<CartItem>();
            var removed = cart.RemoveAll(c =>
                c.RoomId == roomId &&
                c.CheckIn.Date == ci.Date &&
                c.CheckOut.Date == co.Date) > 0;

            HttpContext.Session.SetObject(SESSION_CART_KEY, cart);
            if (removed) TempData["Success"] = "Item removed from cart.";
            return Redirect("/Customer/Cart");
        }

        // POST: /Customer/Cart/Clear
        [HttpPost("Clear")]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(SESSION_CART_KEY);
            TempData["Success"] = "Cart cleared.";
            return Redirect("/Customer/Cart");
        }
    }
}
