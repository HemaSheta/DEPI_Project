using Depi_Project.Helpers;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Depi_Project.Data.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;

namespace Depi_Project.Controllers.Customer
{
    [Authorize]
    [Route("Customer/[controller]/[action]")]
    public class PaymentsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBookingService _bookingService;
        private readonly IUnitOfWork _unitOfWork;

        // session cart key must match your cart controller
        private const string SESSION_CART_KEY = "reservation_cart_v1";

        public PaymentsController(
            UserManager<IdentityUser> userManager,
            IBookingService bookingService,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _bookingService = bookingService;
            _unitOfWork = unitOfWork;
        }

        // GET: /Customer/Payments/CheckoutFromCart
        // This will create bookings (PaymentStatus = Not Paid), clear the cart and return JSON (AJAX) with redirect url.
        [HttpGet]
        public async Task<IActionResult> CheckoutFromCart()
        {
            try
            {
                var cart = HttpContext.Session.GetObject<List<CartItem>>(SESSION_CART_KEY) ?? new List<CartItem>();
                if (!cart.Any())
                {
                    return Json(new { error = "CartEmpty", message = "Your cart is empty." });
                }

                // 1) Validate cart internal overlap (customer rule: cannot book multiple overlapping items)
                for (int i = 0; i < cart.Count; i++)
                {
                    for (int j = i + 1; j < cart.Count; j++)
                    {
                        var a = cart[i];
                        var b = cart[j];
                        bool overlap = a.CheckIn < b.CheckOut && a.CheckOut > b.CheckIn;
                        if (overlap)
                        {
                            return Json(new { error = "OverlapInCart", message = "Cart contains two items that overlap in dates. Remove one." });
                        }
                    }
                }

                // 2) Validate availability against current bookings in DB
                foreach (var c in cart)
                {
                    if (!_bookingService.IsRoomAvailable(c.RoomId, c.CheckIn, c.CheckOut))
                    {
                        return Json(new { error = "RoomUnavailable", message = $"Room #{c.RoomNum} is no longer available for the selected dates." });
                    }
                }

                // 3) Validate user doesn't have conflicting booking
                var identityId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(identityId))
                    return Json(new { error = "NotAuthenticated", message = "Could not determine logged-in user. Please re-login." });

                var userBookings = _bookingService.GetAllBookings()
                    .Where(b => b.IdentityUserId == identityId)
                    .ToList();

                foreach (var c in cart)
                {
                    foreach (var ub in userBookings)
                    {
                        bool overlap = c.CheckIn < ub.CheckOutTime && c.CheckOut > ub.CheckTime;
                        if (overlap)
                        {
                            return Json(new { error = "UserOverlap", message = "You already have a booking that overlaps one of the cart items. Remove the conflicting item." });
                        }
                    }
                }

                // Fetch identity user entity (important: assign both FK and navigation)
                var identityUser = await _userManager.FindByIdAsync(identityId);
                if (identityUser == null)
                {
                    return Json(new { error = "NotFoundUser", message = "Logged-in user not found." });
                }

                // 4) All validations passed -> create bookings inside a transaction
                using (var tx = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in cart)
                        {
                            var booking = new Booking
                            {
                                RoomId = item.RoomId,
                                IdentityUserId = identityId,     // set FK explicitly
                                IdentityUser = identityUser,     // set navigation object too (ensures EF tracks relationship)
                                CheckTime = item.CheckIn,
                                CheckOutTime = item.CheckOut,
                                TotalPrice = item.TotalPrice,
                                PaymentStatus = "Not Paid"
                            };

                            // Validate business rules again for this constructed booking
                            if (!_bookingService.ValidateBooking(booking, out string error))
                            {
                                tx.Rollback();
                                return Json(new { error = "ValidationFailed", message = error });
                            }

                            // Add booking to UoW
                            _unitOfWork.Bookings.Add(booking);
                        }

                        // Save + commit
                        _unitOfWork.Save();
                        tx.Commit();
                    }
                    catch (Exception exTx)
                    {
                        try { tx.Rollback(); } catch { }
                        return Json(new { error = "ServerError", message = $"Server error while creating Bookings. {exTx.Message}" });
                    }
                }

                // 5) Clear cart
                HttpContext.Session.Remove(SESSION_CART_KEY);

                // Return success JSON with redirect url to Success page (client will redirect)
                var successUrl = Url.Action("Success", "Cart", new { area = "" }) ?? "/Customer/Cart/Success";
                return Json(new { url = successUrl });
            }
            catch (Exception ex)
            {
                return Json(new { error = "ServerError", message = $"Server error while creating Bookings. {ex.Message}" });
            }
        }
    }
}
