using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Depi_Project.Data.UnitOfWork;
using Depi_Project.Helpers;
using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Customer
{
    [Authorize]
    [Route("Customer/[controller]/[action]")]
    public class PaymentsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBookingService _bookingService;
        private readonly IUnitOfWork _unitOfWork;

        private const string SESSION_CART_PREFIX = "reservation_cart_v1";

        public PaymentsController(
            UserManager<IdentityUser> userManager,
            IBookingService bookingService,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _bookingService = bookingService;   // FIXED
            _unitOfWork = unitOfWork;
        }

        private string GetCartKey()
        {
            var uid = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(uid))
                return $"{SESSION_CART_PREFIX}_user_{uid}";

            var sid = HttpContext?.Session?.Id;
            if (string.IsNullOrEmpty(sid))
            {
                HttpContext?.Session?.SetString("__session_init", "1");
                sid = HttpContext?.Session?.Id ?? Guid.NewGuid().ToString();
            }

            return $"{SESSION_CART_PREFIX}_session_{sid}";
        }

        [HttpGet]
        public IActionResult CheckoutFromCart()
        {
            try
            {
                var cart = HttpContext.Session.GetObject<List<CartItem>>(GetCartKey()) ?? new List<CartItem>();
                if (!cart.Any())
                    return BadRequest(new { error = "CartEmpty", message = "Your cart is empty." });

                // -------- 1. OVERLAP INSIDE CART --------
                for (int i = 0; i < cart.Count; i++)
                {
                    for (int j = i + 1; j < cart.Count; j++)
                    {
                        var a = cart[i];
                        var b = cart[j];
                        if (a.CheckIn < b.CheckOut && a.CheckOut > b.CheckIn)
                            return BadRequest(new { error = "OverlapInCart", message = "Cart contains overlapping dates." });
                    }
                }

                // -------- 2. CHECK ROOM AVAILABILITY --------
                foreach (var c in cart)
                {
                    if (!_bookingService.IsRoomAvailable(c.RoomId, c.CheckIn, c.CheckOut))
                    {
                        return BadRequest(new
                        {
                            error = "RoomUnavailable",
                            message = $"Room #{c.RoomNum} is no longer available."
                        });
                    }
                }

                // -------- 3. USER OVERLAP (IGNORE CANCELED BOOKINGS) --------
                var identityId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(identityId))
                    return BadRequest(new { error = "NotAuthenticated", message = "Login again." });

                var userBookings = _bookingService.GetAllBookings()
                    .Where(b => b.IdentityUserId == identityId && b.Status != "Canceled")
                    .ToList();

                foreach (var c in cart)
                {
                    foreach (var ub in userBookings)
                    {
                        if (c.CheckIn < ub.CheckOutTime && c.CheckOut > ub.CheckTime)
                            return BadRequest(new
                            {
                                error = "UserOverlap",
                                message = "You already have a booking that overlaps one of the selected dates."
                            });
                    }
                }

                // -------- 4. CREATE BOOKINGS (TRANSACTION) --------
                using (var tx = _unitOfWork.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in cart)
                        {
                            var booking = new Booking
                            {
                                RoomId = item.RoomId,
                                IdentityUserId = identityId,
                                CheckTime = item.CheckIn,
                                CheckOutTime = item.CheckOut,
                                TotalPrice = item.TotalPrice,
                                PaymentStatus = "Not Paid",
                                Status = "Pending"
                            };

                            if (!_bookingService.ValidateBooking(booking, out string errorMsg))
                            {
                                tx.Rollback();
                                return BadRequest(new { error = "ValidationFailed", message = errorMsg });
                            }

                            if (!_bookingService.CreateBooking(booking))
                            {
                                tx.Rollback();
                                return StatusCode(500, new { error = "CreateFailed", message = "Could not create booking." });
                            }
                        }

                        _unitOfWork.Save();
                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }
                        return StatusCode(500, new { error = "ServerError", message = ex.Message });
                    }
                }

                // -------- 5. CLEAR CART --------
                HttpContext.Session.Remove(GetCartKey());

                // Redirect URL
                var url = $"{Request.Scheme}://{Request.Host}/Customer/Cart/Success";
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "ServerError", message = ex.Message });
            }
        }
    }
}
