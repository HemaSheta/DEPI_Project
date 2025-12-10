using Depi_Project.Data.UnitOfWork;
using Depi_Project.Services.Interfaces;
using Depi_Project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Depi_Project.Controllers.Admin
{
    [Authorize]
    [AdminOnly]
    [Route("Admin/[controller]")]
    public class BookingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingService _bookingService;

        public BookingController(IUnitOfWork unitOfWork, IBookingService bookingService)
        {
            _unitOfWork = unitOfWork;
            _bookingService = bookingService;
        }

        // DTO for the view
        public class BookingListItem
        {
            public Booking Booking { get; set; }
            public string Email { get; set; } = "";
            public UserProfile? Profile { get; set; }
        }

        // GET: /Admin/Booking
        // Optional parameters: search (email or phone), from, to, roomId, paymentStatus, status
        [HttpGet("")]
        public IActionResult Index(string? search, string? from, string? to, int? roomId, string? paymentStatus, string? status)
        {
            DateTime? dtFrom = null, dtTo = null;
            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, out var tmpf)) dtFrom = tmpf.Date;
            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, out var tmpt)) dtTo = tmpt.Date;

            var ctx = _unitOfWork.Context;

            // Start query: include related Room -> RoomType and IdentityUser
            var q = ctx.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.IdentityUser)
                .AsQueryable();

            // Date range filtering: bookings that intersect [dtFrom, dtTo]
            if (dtFrom.HasValue && dtTo.HasValue)
            {
                var a = dtFrom.Value;
                var bDate = dtTo.Value.AddDays(1).AddTicks(-1);
                q = q.Where(bk => bk.CheckTime <= bDate && bk.CheckOutTime >= a);
            }
            else if (dtFrom.HasValue)
            {
                var a = dtFrom.Value;
                q = q.Where(bk => bk.CheckOutTime >= a);
            }
            else if (dtTo.HasValue)
            {
                var bDate = dtTo.Value;
                q = q.Where(bk => bk.CheckTime <= bDate);
            }

            // Filter by room id if provided
            if (roomId.HasValue)
            {
                q = q.Where(bk => bk.RoomId == roomId.Value);
            }

            // Filter by payment status (Paid / Not Paid / Pending / etc.)
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                var ps = paymentStatus.Trim();
                q = q.Where(bk => bk.PaymentStatus == ps);
            }

            // NEW: Filter by booking workflow status (Approved | Canceled | Pending)
            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim();
                q = q.Where(bk => bk.Status == st);
            }

            // Search by email or phone
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();

                // IMPORTANT: use ctx.UserProfiles (IQueryable) so EF can translate the subquery.
                var profiles = ctx.UserProfiles;

                q = q.Where(b =>
                    (b.IdentityUser != null && b.IdentityUser.Email != null && b.IdentityUser.Email.ToLower().Contains(s))
                    ||
                    profiles.Any(up => up.IdentityUserId == b.IdentityUserId && up.Phone != null && up.Phone.ToLower().Contains(s))
                );
            }

            // Order after filtering
            q = q.OrderByDescending(b => b.CheckTime);

            var list = q.ToList();

            // Build the view model items
            var vm = list.Select(b => new BookingListItem
            {
                Booking = b,
                Email = b.IdentityUser?.Email ?? "",
                Profile = ctx.UserProfiles.FirstOrDefault(up => up.IdentityUserId == b.IdentityUserId)
            }).ToList();

            // Prepare filter dropdowns/lists for the view
            ViewBag.Rooms = _unitOfWork.Rooms.GetAll().ToList();
            ViewBag.PaymentStatuses = new List<string> { "Paid", "Not Paid", "Pending" };
            ViewBag.Statuses = new List<string> { "Pending", "Approved", "Canceled" };

            ViewBag.Search = search ?? "";
            ViewBag.From = dtFrom?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.To = dtTo?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.RoomId = roomId;
            ViewBag.PaymentStatus = paymentStatus ?? "";
            ViewBag.Status = status ?? "";

            // Return the view model list
            return View("~/Views/Admin/Booking/Index.cshtml", vm);
        }

        // GET: /Admin/Booking/Details/5
        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            var booking = _bookingService.GetBookingById(id);
            if (booking == null) return NotFound();

            // Get user profile
            var profile = _unitOfWork.UserProfiles.GetAll().FirstOrDefault(up => up.IdentityUserId == booking.IdentityUserId);
            ViewBag.Profile = profile;

            return View("~/Views/Admin/Booking/Details.cshtml", booking);
        }

        // GET: /Admin/Booking/EditPayment/5
        [HttpGet("EditPayment/{id}")]
        public IActionResult EditPayment(int id)
        {
            var booking = _booking_service_or_null(id);
            if (booking == null) return NotFound();
            return View("~/Views/Admin/Booking/EditPayment.cshtml", booking);
        }

        // POST: /Admin/Booking/EditPayment/5
        [HttpPost("EditPayment/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult EditPaymentPost(int id, string paymentStatus)
        {
            var booking = _booking_service_or_null(id);
            if (booking == null) return NotFound();

            booking.PaymentStatus = paymentStatus;

            // If admin marks payment as Paid -> mark booking Approved automatically
            if (string.Equals(paymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                booking.Status = "Approved";
            }
            else
            {
                // If payment is Pending or Not Paid -> keep status Pending (unless explicitly canceled)
                if (!string.Equals(booking.Status, "Canceled", StringComparison.OrdinalIgnoreCase))
                {
                    booking.Status = "Pending";
                }
            }

            _unitOfWork.Bookings.Update(booking);
            _unitOfWork.Save();

            TempData["Success"] = "Payment status updated.";
            return Redirect("/Admin/Booking");
        }

        // POST: /Admin/Booking/Cancel/5  (admin soft-cancel)
        [HttpPost("Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var booking = _booking_service_or_null(id);
            if (booking == null) return NotFound();

            // Soft cancel via service
            bool ok = _bookingService.CancelBookingSoft(id);
            if (!ok)
            {
                TempData["Error"] = "Could not cancel booking.";
            }
            else
            {
                TempData["Success"] = "Booking canceled.";
            }

            return Redirect("/Admin/Booking");
        }

        // small helper to avoid repeating the service call when later we may want to include nav props
        private Booking? _booking_service_or_null(int id)
        {
            return _bookingService.GetBookingById(id);
        }
    }
}
