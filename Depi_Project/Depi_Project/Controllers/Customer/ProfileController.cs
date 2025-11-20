using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Customer
{
    [Authorize]
    [Route("Customer/[controller]")]
    public class ProfileController : Controller
    {
        private readonly IUserProfileService _profileService;
        private readonly IBookingService _bookingService;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(
            IUserProfileService profileService,
            IBookingService bookingService,
            UserManager<IdentityUser> userManager)
        {
            _profileService = profileService;
            _bookingService = bookingService;
            _userManager = userManager;
        }

        // GET: /Customer/Profile
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (identityUserId == null)
                return Challenge();

            // Load the IdentityUser
            var identityUser = await _userManager.FindByIdAsync(identityUserId);

            // Load or create UserProfile
            var profile = _profileService.GetProfileByIdentityId(identityUserId);
            if (profile == null)
            {
                profile = new UserProfile
                {
                    IdentityUserId = identityUserId,
                    FirstName = "",
                    LastName = "",
                    Phone = "",
                    Address = ""
                };
                _profileService.CreateProfile(profile);
            }

            profile.IdentityUser = identityUser;

            // Bookings
            var all = _bookingService.GetAllBookings()
                .Where(b => b.IdentityUserId == identityUserId)
                .OrderBy(b => b.CheckTime)
                .ToList();

            var today = DateTime.Today;
            var upcoming = all.Where(b => b.CheckTime.Date >= today).ToList();
            var past = all.Where(b => b.CheckTime.Date < today).ToList();

            var vm = new ProfileViewModel
            {
                Profile = profile,
                UpcomingBookings = upcoming,
                PastBookings = past
            };

            return View("~/Views/Customer/Profile/Index.cshtml", vm);
        }

        // GET: /Customer/Profile/Edit
        [HttpGet("Edit")]
        public async Task<IActionResult> Edit()
        {
            var id = _userManager.GetUserId(User);
            var profile = _profileService.GetProfileByIdentityId(id);

            if (profile == null)
                return RedirectToAction("Index");

            return View("~/Views/Customer/Profile/Edit.cshtml", profile);
        }

        // POST: /Customer/Profile/Edit
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserProfile model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the errors and try again.";
                return View("~/Views/Customer/Profile/Edit.cshtml", model);
            }

            var existing = _profileService.GetProfileByIdentityId(model.IdentityUserId);
            if (existing == null)
            {
                TempData["Error"] = "Profile not found.";
                return RedirectToAction("Index");
            }

            existing.FirstName = model.FirstName;
            existing.LastName = model.LastName;
            existing.Phone = model.Phone;
            existing.Address = model.Address;

            _profileService.UpdateProfile(existing);

            TempData["Success"] = "Profile updated successfully!";

            return RedirectToAction("Index");
        }
    }
}
