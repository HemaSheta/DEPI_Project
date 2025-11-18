using Depi_Project.Services.Interfaces;
using Depi_Project.Models;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Admin
{
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IRoomTypeService _roomTypeService;

        public RoomController(IRoomService roomService, IRoomTypeService roomTypeService)
        {
            _roomService = roomService;
            _roomTypeService = roomTypeService;
        }

        public IActionResult Index()
        {
            var rooms = _roomService.GetAllRooms();
            return View(rooms);
        }

        public IActionResult Create()
        {
            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Room room, IFormFile Slide1File, IFormFile Slide2File, IFormFile Slide3File)
        {
            if (ModelState.IsValid)
            {
                string folder = "wwwroot/images/rooms/";

                // Save Slide1
                if (Slide1File != null)
                {
                    string path1 = Path.Combine(folder, Slide1File.FileName);
                    using (var stream = new FileStream(path1, FileMode.Create))
                    {
                        Slide1File.CopyTo(stream);
                    }
                    room.Slide1 = "/images/rooms/" + Slide1File.FileName;
                }

                // Save Slide2
                if (Slide2File != null)
                {
                    string path2 = Path.Combine(folder, Slide2File.FileName);
                    using (var stream = new FileStream(path2, FileMode.Create))
                    {
                        Slide2File.CopyTo(stream);
                    }
                    room.Slide2 = "/images/rooms/" + Slide2File.FileName;
                }

                // Save Slide3
                if (Slide3File != null)
                {
                    string path3 = Path.Combine(folder, Slide3File.FileName);
                    using (var stream = new FileStream(path3, FileMode.Create))
                    {
                        Slide3File.CopyTo(stream);
                    }
                    room.Slide3 = "/images/rooms/" + Slide3File.FileName;
                }

                _roomService.CreateRoom(room);
                return RedirectToAction("Index");
            }

            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View(room);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Room room, IFormFile? Slide1File, IFormFile? Slide2File, IFormFile? Slide3File)
        {
            if (ModelState.IsValid)
            {
                string folder = "wwwroot/images/rooms/";

                if (Slide1File != null)
                {
                    string path = Path.Combine(folder, Slide1File.FileName);
                    using var stream = new FileStream(path, FileMode.Create);
                    Slide1File.CopyTo(stream);
                    room.Slide1 = "/images/rooms/" + Slide1File.FileName;
                }

                if (Slide2File != null)
                {
                    string path = Path.Combine(folder, Slide2File.FileName);
                    using var stream = new FileStream(path, FileMode.Create);
                    Slide2File.CopyTo(stream);
                    room.Slide2 = "/images/rooms/" + Slide2File.FileName;
                }

                if (Slide3File != null)
                {
                    string path = Path.Combine(folder, Slide3File.FileName);
                    using var stream = new FileStream(path, FileMode.Create);
                    Slide3File.CopyTo(stream);
                    room.Slide3 = "/images/rooms/" + Slide3File.FileName;
                }

                _roomService.UpdateRoom(room);
                return RedirectToAction("Index");
            }

            ViewBag.RoomTypes = _roomTypeService.GetAllRoomTypes();
            return View(room);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _roomService.DeleteRoom(id);
            return RedirectToAction("Index");
        }
    }
}
