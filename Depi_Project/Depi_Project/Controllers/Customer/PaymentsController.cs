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

        // POST: Customer/Payments/CreateCheckoutSession
        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
        {
            // 1. Validate request
            if (request.totalPrice <= 0 ||
                string.IsNullOrWhiteSpace(request.checkIn) ||
                string.IsNullOrWhiteSpace(request.checkOut))
            {
                return BadRequest(new { error = "Invalid booking details." });
            }

            // 2. Get logged-in IdentityUserId (string)
            var identityUserId = _userManager.GetUserId(User);

            // 3. Stripe setup
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var domain = $"{Request.Scheme}://{Request.Host}";

            // 4. Stripe checkout session
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
                    { "IdentityUserId", identityUserId },          // ⭐ IMPORTANT
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
