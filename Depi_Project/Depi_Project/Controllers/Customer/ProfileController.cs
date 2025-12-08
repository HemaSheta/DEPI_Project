using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;
using Depi_Project.Models;
using Depi_Project.Data.UnitOfWork;
using Microsoft.AspNetCore.Identity;

namespace Depi_Project.Controllers.Customer
{
    [Authorize]
    [Route("Customer/[controller]")]
    public class ProfileController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(
            IBookingService bookingService,
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager)
        {
            _bookingService = bookingService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET: /Customer/Profile
        [HttpGet("")]
        public IActionResult Index()
        {
            return Bookings();
        }

        // GET: /Customer/Profile/Bookings
        [HttpGet("Bookings")]
        public IActionResult Bookings()
        {
            // Get current user's Identity Id (AspNetUsers.Id)
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                var emptyModelNoUser = new ProfileViewModel
                {
                    Profile = null,
                    UpcomingBookings = Enumerable.Empty<Booking>().ToList(),
                    PastBookings = Enumerable.Empty<Booking>().ToList()
                };

                return View("~/Views/Customer/Profile/Index.cshtml", emptyModelNoUser);
            }

            // Auto-update expired unpaid bookings (CheckOutTime < today -> Not Paid if not Paid)
            try
            {
                var ctx = _unitOfWork.Context;
                var today = DateTime.Today;
                var expired = ctx.Bookings
                                 .Where(b => b.CheckOutTime.Date < today && b.PaymentStatus != "Paid" && b.PaymentStatus != "Not Paid")
                                 .ToList();
                if (expired.Any())
                {
                    foreach (var eb in expired)
                    {
                        eb.PaymentStatus = "Not Paid";
                        ctx.Bookings.Update(eb);
                    }
                    _unitOfWork.Save();
                }
            }
            catch
            {
                // Non-fatal; if updating fails we still show bookings
            }

            // Get all bookings (BookingService returns navigation properties)
            var all = _bookingService.GetAllBookings() ?? Enumerable.Empty<Booking>();

            // Filter only bookings belonging to the current user
            var myBookings = all.Where(b => string.Equals(b.IdentityUserId, userId, StringComparison.OrdinalIgnoreCase));

            var today2 = DateTime.Today;

            // Upcoming: check-out is today or in the future (not passed yet), sort ascending by check-in
            var upcoming = myBookings
                .Where(b => b.CheckOutTime.Date >= today2)
                .OrderBy(b => b.CheckTime)
                .ToList();

            // Past: check-out date is before today
            var past = myBookings
                .Where(b => b.CheckOutTime.Date < today2)
                .OrderByDescending(b => b.CheckOutTime)
                .ToList();

            // Load or create UserProfile
            var profile = _unitOfWork.UserProfiles
                .GetAll()
                .FirstOrDefault(p => p.IdentityUserId == userId);

            // Try to attach IdentityUser to profile for view convenience
            IdentityUser identityUser = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();

            if (profile == null)
            {
                // create lightweight object so view doesn't NRE
                profile = new UserProfile
                {
                    IdentityUserId = userId,
                    IdentityUser = identityUser
                };
            }
            else
            {
                // ensure navigation property has user (useful if view accesses Email)
                if (profile.IdentityUser == null)
                    profile.IdentityUser = identityUser;
            }

            var model = new ProfileViewModel
            {
                Profile = profile,
                UpcomingBookings = upcoming,
                PastBookings = past
            };

            return View("~/Views/Customer/Profile/Index.cshtml", model);
        }

        // GET: /Customer/Profile/Edit
        [HttpGet("Edit")]
        public IActionResult Edit()
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            var profile = _unitOfWork.UserProfiles
                .GetAll()
                .FirstOrDefault(p => p.IdentityUserId == userId);

            // If not exists, create a new one to be edited
            if (profile == null)
            {
                profile = new UserProfile
                {
                    IdentityUserId = userId
                };
            }

            // Try to get identity email for display
            var identityUser = _userManager.FindByIdAsync(userId).GetAwaiter().GetResult();
            if (identityUser != null)
            {
                profile.IdentityUser = identityUser;
            }

            return View("~/Views/Customer/Profile/Edit.cshtml", profile);
        }

        // POST: /Customer/Profile/Edit
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserProfile form)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index");

            // Sanity: enforce IdentityUserId
            form.IdentityUserId = userId;

            var existing = _unitOfWork.UserProfiles
                .GetAll()
                .FirstOrDefault(p => p.IdentityUserId == userId);

            if (existing == null)
            {
                // Create new
                _unitOfWork.UserProfiles.Add(form);
            }
            else
            {
                // Update fields we allow editing
                existing.FirstName = form.FirstName;
                existing.LastName = form.LastName;
                existing.Phone = form.Phone;
                _unitOfWork.UserProfiles.Update(existing);
            }

            _unitOfWork.Save();

            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Bookings");
        }
    }
}
