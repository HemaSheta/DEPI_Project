using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Depi_Project.Helpers;
using Depi_Project.Data.UnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Depi_Project.Controllers.Customer
{
    [Authorize]
    [Route("Customer/[controller]")]
    public class CartController : Controller
    {
        private const string SESSION_CART_KEY = "reservation_cart_v1";

        private readonly IRoomService _roomService;
        private readonly IBookingService _bookingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CartController> _logger;

        public CartController(
            IRoomService roomService,
            IBookingService bookingService,
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            ILogger<CartController> logger)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (!_booking_service_safe_check(room.RoomId, ci, co))
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

            if (stay)
            {
                TempData["Success"] = "Added to cart.";
                return Redirect("/Customer/Room");
            }

            TempData["Success"] = "Added to cart.";
            return Redirect("/Customer/Cart");
        }

        // small helper to call booking availability safely with try/catch and log if necessary
        private bool _booking_service_safe_check(int roomId, DateTime ci, DateTime co)
        {
            try
            {
                return _bookingService.IsRoomAvailable(roomId, ci, co);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IsRoomAvailable check failed for room {RoomId} {CheckIn}->{CheckOut}", roomId, ci, co);
                // in case of error assume not available to avoid double bookings
                return false;
            }
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

        // POST: /Customer/Cart/Checkout
        // Returns JSON { success: true, redirect: "..."} on success. Detailed error JSON on failure (development only).
        [HttpPost("Checkout")]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(SESSION_CART_KEY) ?? new List<CartItem>();
            if (!cart.Any())
                return BadRequest(new { error = "Your cart is empty." });

            // 1) Validate duplicates or overlapping same-room entries in cart
            for (int i = 0; i < cart.Count; i++)
            {
                for (int j = i + 1; j < cart.Count; j++)
                {
                    var a = cart[i];
                    var b = cart[j];

                    bool overlap = a.CheckIn < b.CheckOut && a.CheckOut > b.CheckIn;

                    if (a.RoomId == b.RoomId && overlap)
                    {
                        return BadRequest(new { error = "Cart contains the same room for overlapping dates. Please remove duplicates." });
                    }

                    // Business rule: prevent booking multiple rooms with overlapping dates
                    if (overlap)
                    {
                        return BadRequest(new { error = "Cart contains items with overlapping dates. You cannot book multiple rooms for overlapping dates." });
                    }
                }
            }

            // 2) Validate availability vs DB bookings
            foreach (var c in cart)
            {
                bool avail;
                try
                {
                    avail = _bookingService.IsRoomAvailable(c.RoomId, c.CheckIn, c.CheckOut);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when checking availability for room {RoomId} {CheckIn}->{CheckOut}", c.RoomId, c.CheckIn, c.CheckOut);
                    return StatusCode(500, new { error = "Error while checking availability. Please try again." });
                }

                if (!avail)
                {
                    return BadRequest(new { error = $"Room #{c.RoomNum} is no longer available for the selected dates." });
                }
            }

            // 3) Validate user's existing bookings do not overlap
            var identityId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(identityId))
                return BadRequest(new { error = "Can't determine user identity. Please re-login." });

            IEnumerable<Booking> userBookings;
            try
            {
                userBookings = _bookingService.GetAllBookings().Where(b => b.IdentityUserId == identityId).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user bookings for identity {IdentityId}", identityId);
                return StatusCode(500, new { error = "Server error while validating user bookings." });
            }

            foreach (var c in cart)
            {
                foreach (var ub in userBookings)
                {
                    bool overlap = c.CheckIn < ub.CheckOutTime && c.CheckOut > ub.CheckTime;
                    if (overlap)
                        return BadRequest(new { error = "You already have a booking that overlaps one of the cart items. Remove the conflicting item." });
                }
            }

            // 4) Persist bookings inside a DB transaction using UnitOfWork
            try
            {
                using var tx = _unitOfWork.BeginTransaction();
                try
                {
                    foreach (var c in cart)
                    {
                        var booking = new Booking
                        {
                            RoomId = c.RoomId,
                            IdentityUserId = identityId,
                            CheckTime = c.CheckIn,
                            CheckOutTime = c.CheckOut,
                            TotalPrice = c.TotalPrice,
                            PaymentStatus = "Not Paid",
                            // Ensure navigation props are null to avoid EF trying to attach them unexpectedly
                            Room = null,
                            IdentityUser = null
                        };

                        // Add booking via UnitOfWork repository
                        _unitOfWork.Bookings.Add(booking);
                    }

                    // Save + commit
                    _unitOfWork.Save();
                    tx.Commit();
                }
                catch (DbUpdateException dbEx)
                {
                    try { tx.Rollback(); } catch { /* ignore rollback failure */ }

                    // Capture inner exception if present
                    var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                    _logger.LogError(dbEx, "DbUpdateException while creating bookings for user {UserId}. Inner: {Inner}", identityId, inner);

                    // Return inner message to help debugging (development). Remove in production.
                    return StatusCode(500, new { error = "Server error while creating Bookings. " + inner });
                }
                catch (Exception exInner)
                {
                    try { tx.Rollback(); } catch { /* ignore rollback failure */ }

                    _logger.LogError(exInner, "Failed to create bookings during checkout for user {UserId}. Cart count={Count}", identityId, cart.Count);
                    return StatusCode(500, new { error = "Server error while creating Bookings. " + exInner.Message });
                }
            }
            catch (Exception exOuter)
            {
                // Could not open transaction or some other error
                _logger.LogError(exOuter, "Failed to begin DB transaction for checkout for user {UserId}.", identityId);
                return StatusCode(500, new { error = "Server error while creating Bookings. " + exOuter.Message });
            }

            // 5) If we've reached here, commit succeeded — clear session cart
            try
            {
                HttpContext.Session.Remove(SESSION_CART_KEY);
            }
            catch (Exception ex)
            {
                // Non-fatal: log but continue to success (cart may still be present; user can manually clear later)
                _logger.LogWarning(ex, "Failed to clear session cart after successful booking for user {UserId}.", identityId);
            }

            // 6) success JSON — front-end will redirect
            return Json(new { success = true, redirect = Url.Action("Success", "Cart", new { area = "" }) ?? "/Customer/Cart/Success" });
        }

        // GET: /Customer/Cart/Success
        [HttpGet("Success")]
        public IActionResult Success()
        {
            return View("~/Views/Customer/Cart/Success.cshtml");
        }
    }
}
