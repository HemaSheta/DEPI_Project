using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("")]
        public IActionResult Index()
        {
            var rooms = _roomService.GetAllRooms();
            return View("~/Views/Admin/Room/Index.cshtml", rooms);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View("~/Views/Admin/Room/Create.cshtml");
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Room room, IFormFile Slide1File, IFormFile Slide2File, IFormFile Slide3File)
        {
            ModelState.Remove("RoomType");
            ModelState.Remove("Slide1");
            ModelState.Remove("Slide2");
            ModelState.Remove("Slide3");

            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
                return View("~/Views/Admin/Room/Create.cshtml", room);
            }

            if (Slide1File == null)
            {
                ModelState.AddModelError("Slide1File", "Slide 1 is required.");
                ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
                return View("~/Views/Admin/Room/Create.cshtml", room);
            }

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/rooms");
            Directory.CreateDirectory(folder);

            // Slide1
            var f1 = Path.GetFileName(Slide1File.FileName);
            var p1 = Path.Combine(folder, f1);
            using (var st = new FileStream(p1, FileMode.Create))
                Slide1File.CopyTo(st);
            room.Slide1 = "/images/rooms/" + f1;

            // Slide2
            if (Slide2File != null)
            {
                var f2 = Path.GetFileName(Slide2File.FileName);
                var p2 = Path.Combine(folder, f2);
                using (var st = new FileStream(p2, FileMode.Create))
                    Slide2File.CopyTo(st);
                room.Slide2 = "/images/rooms/" + f2;
            }

            // Slide3
            if (Slide3File != null)
            {
                var f3 = Path.GetFileName(Slide3File.FileName);
                var p3 = Path.Combine(folder, f3);
                using (var st = new FileStream(p3, FileMode.Create))
                    Slide3File.CopyTo(st);
                room.Slide3 = "/images/rooms/" + f3;
            }

            _roomService.CreateRoom(room);
            return Redirect("/Admin/Room");
        }


        // ============================
        // EDIT — FIXED HERE 🔥
        // ============================

        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View("~/Views/Admin/Room/Edit.cshtml", room);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Room formModel, IFormFile? Slide1File, IFormFile? Slide2File, IFormFile? Slide3File)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            // Update basic fields
            room.RoomNum = formModel.RoomNum;
            room.Status = formModel.Status;
            room.Description = formModel.Description;
            room.RoomTypeId = formModel.RoomTypeId;

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/rooms");
            Directory.CreateDirectory(folder);

            // Slide1
            if (Slide1File != null)
            {
                var f1 = Path.GetFileName(Slide1File.FileName);
                var p1 = Path.Combine(folder, f1);
                using (var st = new FileStream(p1, FileMode.Create))
                    Slide1File.CopyTo(st);
                room.Slide1 = "/images/rooms/" + f1;
            }

            // Slide2
            if (Slide2File != null)
            {
                var f2 = Path.GetFileName(Slide2File.FileName);
                var p2 = Path.Combine(folder, f2);
                using (var st = new FileStream(p2, FileMode.Create))
                    Slide2File.CopyTo(st);
                room.Slide2 = "/images/rooms/" + f2;
            }

            // Slide3
            if (Slide3File != null)
            {
                var f3 = Path.GetFileName(Slide3File.FileName);
                var p3 = Path.Combine(folder, f3);
                using (var st = new FileStream(p3, FileMode.Create))
                    Slide3File.CopyTo(st);
                room.Slide3 = "/images/rooms/" + f3;
            }

            _roomService.UpdateRoom(room);
            return Redirect("/Admin/Room");
        }


        // ============================
        // DELETE — FIXED HERE 🔥
        // ============================

        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            return View("~/Views/Admin/Room/Delete.cshtml", room);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _roomService.DeleteRoom(id);
            return Redirect("/Admin/Room");
        }
    }
}
