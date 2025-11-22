using Depi_Project.Helpers;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Depi_Project.Controllers.Customer
{
    [Authorize]
    [Route("Customer/[controller]/[action]")]
    public class PaymentsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly StripeSettings _stripeSettings;
        private readonly IBookingService _bookingService;

        public PaymentsController(
            UserManager<IdentityUser> userManager,
            IOptions<StripeSettings> stripeOptions,
            IBookingService bookingService)
        {
            _userManager = userManager;
            _stripeSettings = stripeOptions.Value;
            _bookingService = bookingService;
        }

        // ===========================
        //   CART CHECKOUT (Stripe)
        // ===========================
        [HttpGet]
        public IActionResult CheckoutFromCart()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("reservation_cart_v1")
                       ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return Redirect("/Customer/Cart");
            }

            // 1) Validate cart items: no overlapping between items (customer rule)
            for (int i = 0; i < cart.Count; i++)
            {
                for (int j = i + 1; j < cart.Count; j++)
                {
                    var a = cart[i];
                    var b = cart[j];

                    bool overlap = a.CheckIn < b.CheckOut && a.CheckOut > b.CheckIn;
                    if (overlap)
                    {
                        TempData["Error"] = "Cart contains two items that overlap in dates. You cannot book multiple rooms for overlapping dates.";
                        return Redirect("/Customer/Cart");
                    }
                }
            }

            // 2) Validate availability for each cart item against existing bookings
            foreach (var c in cart)
            {
                if (!_bookingService.IsRoomAvailable(c.RoomId, c.CheckIn, c.CheckOut))
                {
                    TempData["Error"] = $"Room #{c.RoomNum} is no longer available for the selected dates.";
                    return Redirect("/Customer/Cart");
                }
            }

            // 3) Validate that the user doesn't have existing bookings overlapping any cart item
            var identityId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(identityId))
            {
                foreach (var c in cart)
                {
                    var userBookings = _bookingService.GetAllBookings()
                        .Where(b => b.IdentityUserId == identityId);

                    foreach (var ub in userBookings)
                    {
                        bool overlap = c.CheckIn < ub.CheckOutTime && c.CheckOut > ub.CheckTime;
                        if (overlap)
                        {
                            TempData["Error"] = "You already have a booking that overlaps one of the cart items. Please remove the conflicting item.";
                            return Redirect("/Customer/Cart");
                        }
                    }
                }
            }

            // Passed validation -> create Stripe Session
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            var domain = $"{Request.Scheme}://{Request.Host}";

            // Metadata (compact)
            var metadata = new Dictionary<string, string>
            {
                { "CartCount", cart.Count.ToString() },
                { "IdentityUserId", identityId ?? "" }
            };

            for (int i = 0; i < cart.Count; i++)
            {
                var c = cart[i];
                metadata[$"item_{i}"] = $"{c.RoomId}|{c.CheckIn:yyyy-MM-dd}|{c.CheckOut:yyyy-MM-dd}|{c.PricePerNight}";
            }

            var lineItems = cart.Select(c =>
            {
                int nights = (c.CheckOut.Date - c.CheckIn.Date).Days;
                if (nights <= 0) nights = 1;
                return new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(c.PricePerNight * nights * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Room {c.RoomNum} - {c.RoomTitle}",
                            Description = $"{nights} nights ({c.CheckIn:yyyy-MM-dd} to {c.CheckOut:yyyy-MM-dd})"
                        }
                    }
                };
            }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = domain + "/Customer/Cart/Success",
                CancelUrl = domain + "/Customer/Cart",
                Metadata = metadata
            };

            var service = new SessionService();
            var session = service.Create(options);

            // Return session id and url (front-end can redirect)
            return Json(new { id = session.Id, url = session.Url });
        }


        // ===============================
        //   DIRECT ROOM CHECKOUT (OLD)
        // ===============================
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
        {
            if (request.totalPrice <= 0 ||
                string.IsNullOrWhiteSpace(request.checkIn) ||
                string.IsNullOrWhiteSpace(request.checkOut))
            {
                return BadRequest(new { error = "Invalid booking details." });
            }

            var identityUserId = _userManager.GetUserId(User);

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },

                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(request.totalPrice * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Room Booking",
                                Description = $"Room ID: {request.roomId}"
                            }
                        }
                    }
                },

                Mode = "payment",

                SuccessUrl = $"{domain}/Customer/Booking/Success",
                CancelUrl = $"{domain}/Customer/Room/Details/{request.roomId}",

                Metadata = new Dictionary<string, string>
                {
                    { "RoomId", request.roomId.ToString() },
                    { "IdentityUserId", identityUserId },
                    { "CheckIn", request.checkIn },
                    { "CheckOut", request.checkOut },
                    { "TotalPrice", request.totalPrice.ToString() }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Json(new { id = session.Id });
        }
    }

    // Helper class to read POST JSON
    public class CheckoutRequest
    {
        public int roomId { get; set; }
        public string checkIn { get; set; }
        public string checkOut { get; set; }
        public float totalPrice { get; set; }
    }
}
