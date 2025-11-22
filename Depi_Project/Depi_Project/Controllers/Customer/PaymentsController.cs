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

        public PaymentsController(
            UserManager<IdentityUser> userManager,
            IOptions<StripeSettings> stripeOptions)
        {
            _userManager = userManager;
            _stripeSettings = stripeOptions.Value;
        }

        // ===========================
        //   CART CHECKOUT (Stripe)
        // ===========================
        [HttpGet]
        public IActionResult CheckoutFromCart()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("reservation_cart_v1")
                       ?? new List<CartItem>();

            if (!cart.Any()) return Redirect("/Customer/Cart");

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            var domain = $"{Request.Scheme}://{Request.Host}";

            // Metadata sent to Stripe
            var metadata = new Dictionary<string, string>
            {
                { "CartCount", cart.Count.ToString() },
                { "UserId", _userManager.GetUserId(User) ?? "" }
            };

            // Compact metadata (RoomId|CheckIn|CheckOut|Price)
            for (int i = 0; i < cart.Count; i++)
            {
                var c = cart[i];
                metadata[$"item_{i}"] =
                    $"{c.RoomId}|{c.CheckIn:yyyy-MM-dd}|{c.CheckOut:yyyy-MM-dd}|{c.PricePerNight}";
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },

                LineItems = cart.Select(c =>
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
                                Name = $"Room {c.RoomNum} - {c.RoomTitle}", // FIX HERE
                                Description = $"{nights} nights ({c.CheckIn:yyyy-MM-dd} to {c.CheckOut:yyyy-MM-dd})"
                            }
                        }
                    };
                }).ToList(),

                Mode = "payment",
                SuccessUrl = domain + "/Customer/Cart/Success",
                CancelUrl = domain + "/Customer/Cart",
                Metadata = metadata
            };

            var service = new SessionService();
            var session = service.Create(options);

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
