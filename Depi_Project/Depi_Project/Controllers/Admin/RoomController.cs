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
            // Remove file-backed properties from ModelState so they don't make ModelState invalid
            // (they will be set after the files are saved)
            ModelState.Remove(nameof(room.Slide1));
            ModelState.Remove(nameof(room.Slide2));
            ModelState.Remove(nameof(room.Slide3));

            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
                return View("~/Views/Admin/Room/Create.cshtml", room);
            }

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/rooms");
            Directory.CreateDirectory(folder);

            // Slide1
            if (Slide1File != null && Slide1File.Length > 0)
            {
                var safeName = Path.GetFileName(Slide1File.FileName);
                string path1 = Path.Combine(folder, safeName);
                using var stream = new FileStream(path1, FileMode.Create);
                Slide1File.CopyTo(stream);
                room.Slide1 = "/images/rooms/" + safeName;
            }

            // Slide2
            if (Slide2File != null && Slide2File.Length > 0)
            {
                var safeName = Path.GetFileName(Slide2File.FileName);
                string path2 = Path.Combine(folder, safeName);
                using var stream = new FileStream(path2, FileMode.Create);
                Slide2File.CopyTo(stream);
                room.Slide2 = "/images/rooms/" + safeName;
            }

            // Slide3
            if (Slide3File != null && Slide3File.Length > 0)
            {
                var safeName = Path.GetFileName(Slide3File.FileName);
                string path3 = Path.Combine(folder, safeName);
                using var stream = new FileStream(path3, FileMode.Create);
                Slide3File.CopyTo(stream);
                room.Slide3 = "/images/rooms/" + safeName;
            }

            _roomService.CreateRoom(room);
            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View("~/Views/Admin/Room/Edit.cshtml", room);
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Room room, IFormFile? Slide1File, IFormFile? Slide2File, IFormFile? Slide3File)
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/rooms");
            Directory.CreateDirectory(folder);

            if (Slide1File != null && Slide1File.Length > 0)
            {
                var safeName = Path.GetFileName(Slide1File.FileName);
                string path = Path.Combine(folder, safeName);
                using var stream = new FileStream(path, FileMode.Create);
                Slide1File.CopyTo(stream);
                room.Slide1 = "/images/rooms/" + safeName;
            }

            if (Slide2File != null && Slide2File.Length > 0)
            {
                var safeName = Path.GetFileName(Slide2File.FileName);
                string path = Path.Combine(folder, safeName);
                using var stream = new FileStream(path, FileMode.Create);
                Slide2File.CopyTo(stream);
                room.Slide2 = "/images/rooms/" + safeName;
            }

            if (Slide3File != null && Slide3File.Length > 0)
            {
                var safeName = Path.GetFileName(Slide3File.FileName);
                string path = Path.Combine(folder, safeName);
                using var stream = new FileStream(path, FileMode.Create);
                Slide3File.CopyTo(stream);
                room.Slide3 = "/images/rooms/" + safeName;
            }

            _roomService.UpdateRoom(room);
            return RedirectToAction("Index");
        }

        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            return View("~/Views/Admin/Room/Delete.cshtml", room);
        }

        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _roomService.DeleteRoom(id);
            return RedirectToAction("Index");
        }
    }
}
