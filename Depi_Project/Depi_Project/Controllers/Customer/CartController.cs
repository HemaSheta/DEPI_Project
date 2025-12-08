using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Depi_Project.Helpers;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Customer
{
    [Route("Customer/[controller]")]
    public class CartController : Controller
    {
        private const string SESSION_CART_PREFIX = "reservation_cart_v1";
        private readonly IRoomService _roomService;
        private readonly IBookingService _bookingService;

        public CartController(IRoomService roomService, IBookingService bookingService)
        {
            _roomService = roomService;
        _booking_service_or_null: _bookingService = bookingService; // harmless label removed if copy issues occur
            _bookingService = bookingService;
        }

        // Build a cart key scoped to the logged-in user when possible,
        // otherwise fallback to a session-based cart key.
        private string GetCartKey()
        {
            try
            {
                var uid = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(uid))
                    return $"{SESSION_CART_PREFIX}_user_{uid}";

                var sid = HttpContext?.Session?.Id;
                if (string.IsNullOrEmpty(sid))
                {
                    // ensure session exists
                    HttpContext?.Session?.SetString("__session_init", "1");
                    sid = HttpContext?.Session?.Id ?? Guid.NewGuid().ToString();
                }

                return $"{SESSION_CART_PREFIX}_session_{sid}";
            }
            catch
            {
                // fallback safe key
                return SESSION_CART_PREFIX;
            }
        }

        // GET: /Customer/Cart
        [HttpGet("")]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(GetCartKey()) ?? new List<CartItem>();
            ViewBag.Total = cart.Sum(i => i.TotalPrice);
            return View("~/Views/Customer/Cart/Index.cshtml", cart);
        }

        // GET: /Customer/Cart/Count
        // returns { count: n }
        [HttpGet("Count")]
        public IActionResult Count()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(GetCartKey()) ?? new List<CartItem>();
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

            // ensure checkOut > checkIn
            if (co <= ci)
            {
                TempData["Error"] = "Check-out must be after check-in.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/Customer/Room");
            }

            // check in can't be in the past
            if (ci.Date < DateTime.Today)
            {
                TempData["Error"] = "Check-in cannot be in the past.";
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

            var cartKey = GetCartKey();
            var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey) ?? new List<CartItem>();

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
            HttpContext.Session.SetObject(cartKey, cart);

            // Redirect behavior:
            // stay==true means "Add to Cart and go back to Rooms"
            if (stay)
            {
                TempData["Success"] = "Added to cart.";
                return Redirect("/Customer/Room");
            }

            TempData["Success"] = "Added to cart.";
            return Redirect("/Customer/Cart");
        }

        // POST: /Customer/Cart/Remove
        [HttpPost("Remove")]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int roomId, string checkIn, string checkOut)
        {
            if (!DateTime.TryParse(checkIn, out var ci) || !DateTime.TryParse(checkOut, out var co))
                return Redirect("/Customer/Cart");

            var cartKey = GetCartKey();
            var cart = HttpContext.Session.GetObject<List<CartItem>>(cartKey) ?? new List<CartItem>();
            var removed = cart.RemoveAll(c =>
                c.RoomId == roomId &&
                c.CheckIn.Date == ci.Date &&
                c.CheckOut.Date == co.Date) > 0;

            HttpContext.Session.SetObject(cartKey, cart);
            if (removed) TempData["Success"] = "Item removed from cart.";
            return Redirect("/Customer/Cart");
        }

        // POST: /Customer/Cart/Clear
        [HttpPost("Clear")]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(GetCartKey());
            TempData["Success"] = "Cart cleared.";
            return Redirect("/Customer/Cart");
        }

        // GET: /Customer/Cart/Success
        // This renders the success page after checkout.
        [HttpGet("Success")]
        public IActionResult Success()
        {
            // Optionally the controller can set TempData or ViewBag messages
            // or a booking summary. For now we just show the existing view.
            return View("~/Views/Customer/Cart/Success.cshtml");
        }
    }
}
