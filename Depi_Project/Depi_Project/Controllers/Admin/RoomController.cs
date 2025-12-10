using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;

namespace Depi_Project.Controllers.Admin
{
    [Authorize]
    [AdminOnly]
    [Route("Admin/Room")]
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IRoomTypeService _roomTypeService;

        public RoomController(IRoomService roomService, IRoomTypeService roomTypeService)
        {
            _roomService = roomService;
            _roomTypeService = roomTypeService;
        }

        // ==========================
        // INDEX
        // ==========================
        [HttpGet("")]
        public IActionResult Index()
        {
            var rooms = _roomService.GetAllRooms();
            return View("~/Views/Admin/Room/Index.cshtml", rooms);
        }

        // ==========================
        // CREATE (GET)
        // ==========================
        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View("~/Views/Admin/Room/Create.cshtml");
        }

        // ==========================
        // CREATE (POST)
        // ==========================
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Room room, IFormFile Slide1File, IFormFile? Slide2File, IFormFile? Slide3File)
        {
            // Remove ModelState keys so Slide2 & Slide3 become optional
            ModelState.Remove("RoomType");
            ModelState.Remove("Slide1");
            ModelState.Remove("Slide2");
            ModelState.Remove("Slide3");
            ModelState.Remove("Room.Slide1");
            ModelState.Remove("Room.Slide2");
            ModelState.Remove("Room.Slide3");

            // basic validation
            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
                return View("~/Views/Admin/Room/Create.cshtml", room);
            }

            // Check uniqueness BEFORE saving files or DB work
            bool exists = _roomService.GetAllRooms().Any(r => r.RoomNum == room.RoomNum);
            if (exists)
            {
                ModelState.AddModelError("RoomNum", "Room number already exists.");
                TempData["Error"] = "Room number already exists.";
                ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
                return View("~/Views/Admin/Room/Create.cshtml", room);
            }

            // Required Slide1
            if (Slide1File == null || Slide1File.Length == 0)
            {
                ModelState.AddModelError("Slide1File", "Slide 1 is required.");
                ViewBag.RoomTypes = _roomType_service_or_fallback();
                return View("~/Views/Admin/Room/Create.cshtml", room);
            }

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/rooms");
            Directory.CreateDirectory(folder);

            try
            {
                // SAVE Slide1
                var f1 = Path.GetFileName(Slide1File.FileName);
                var p1 = Path.Combine(folder, f1);
                using (var st = new FileStream(p1, FileMode.Create))
                    Slide1File.CopyTo(st);
                room.Slide1 = "/images/rooms/" + f1;

                // SAVE Slide2 (optional)
                if (Slide2File != null && Slide2File.Length > 0)
                {
                    var f2 = Path.GetFileName(Slide2File.FileName);
                    var p2 = Path.Combine(folder, f2);
                    using (var st = new FileStream(p2, FileMode.Create))
                        Slide2File.CopyTo(st);
                    room.Slide2 = "/images/rooms/" + f2;
                }

                // SAVE Slide3 (optional)
                if (Slide3File != null && Slide3File.Length > 0)
                {
                    var f3 = Path.GetFileName(Slide3File.FileName);
                    var p3 = Path.Combine(folder, f3);
                    using (var st = new FileStream(p3, FileMode.Create))
                        Slide3File.CopyTo(st);
                    room.Slide3 = "/images/rooms/" + f3;
                }

                _roomService.CreateRoom(room);
                TempData["Success"] = "Room created.";
                return Redirect("/Admin/Room");
            }
            catch (Exception ex)
            {
                // If file save or DB fails, surface friendly error
                ModelState.AddModelError("", "An error occurred while saving the room: " + ex.Message);
                TempData["Error"] = "Error creating room.";
                ViewBag.RoomTypes = _roomType_service_or_fallback();
                return View("~/Views/Admin/Room/Create.cshtml", room);
            }
        }

        // ==========================
        // EDIT (GET)
        // ==========================
        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View("~/Views/Admin/Room/Edit.cshtml", room);
        }

        // ==========================
        // EDIT (POST)
        // ==========================
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Room formModel, IFormFile? Slide1File, IFormFile? Slide2File, IFormFile? Slide3File)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            // Check uniqueness of room number (exclude current record)
            bool exists = _roomService.GetAllRooms()
                .Any(r => r.RoomNum == formModel.RoomNum && r.RoomId != id);

            if (exists)
            {
                ModelState.AddModelError("RoomNum", "Room number already exists.");
                TempData["Error"] = "Room number already exists.";
                ViewBag.RoomTypes = _roomType_service_or_fallback();
                return View("~/Views/Admin/Room/Edit.cshtml", room);
            }

            // Update base fields
            room.RoomNum = formModel.RoomNum;
            //room.Status = formModel.Status;
            room.Description = formModel.Description;
            room.RoomTypeId = formModel.RoomTypeId;

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/rooms");
            Directory.CreateDirectory(folder);

            try
            {
                // Replace Slide1
                if (Slide1File != null && Slide1File.Length > 0)
                {
                    var f1 = Path.GetFileName(Slide1File.FileName);
                    var p1 = Path.Combine(folder, f1);
                    using (var st = new FileStream(p1, FileMode.Create))
                        Slide1File.CopyTo(st);
                    room.Slide1 = "/images/rooms/" + f1;
                }

                // Replace Slide2
                if (Slide2File != null && Slide2File.Length > 0)
                {
                    var f2 = Path.GetFileName(Slide2File.FileName);
                    var p2 = Path.Combine(folder, f2);
                    using (var st = new FileStream(p2, FileMode.Create))
                        Slide2File.CopyTo(st);
                    room.Slide2 = "/images/rooms/" + f2;
                }

                // Replace Slide3
                if (Slide3File != null && Slide3File.Length > 0)
                {
                    var f3 = Path.GetFileName(Slide3File.FileName);
                    var p3 = Path.Combine(folder, f3);
                    using (var st = new FileStream(p3, FileMode.Create))
                        Slide3File.CopyTo(st);
                    room.Slide3 = "/images/rooms/" + f3;
                }

                _roomService.UpdateRoom(room);
                TempData["Success"] = "Room updated.";
                return Redirect("/Admin/Room");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the room: " + ex.Message);
                TempData["Error"] = "Error updating room.";
                ViewBag.RoomTypes = _roomType_service_or_fallback();
                return View("~/Views/Admin/Room/Edit.cshtml", room);
            }
        }

        // ==========================
        // DELETE (GET)
        // ==========================
        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            return View("~/Views/Admin/Room/Delete.cshtml", room);
        }

        // ==========================
        // DELETE (POST)
        // ==========================
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _roomService.DeleteRoom(id);
            TempData["Success"] = "Room deleted.";
            return Redirect("/Admin/Room");
        }

        // small helper for robustly supplying RoomTypes when error paths run
        private object _roomType_service_or_fallback()
        {
            try
            {
                return _roomTypeService.GetAllRoomTypes();
            }
            catch
            {
                return Enumerable.Empty<object>();
            }
        }
    }
}
