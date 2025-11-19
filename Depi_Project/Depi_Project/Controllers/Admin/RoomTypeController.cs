using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Admin
{
    [Authorize]
    [AdminOnly]
    public class RoomTypeController : Controller
    {
        private readonly IRoomTypeService _roomTypeService;

        public RoomTypeController(IRoomTypeService roomTypeService)
        {
            _roomTypeService = roomTypeService;
        }

        // GET: /Admin/RoomType/
        public IActionResult Index()
        {
            var types = _roomTypeService.GetAllRoomTypes();
            return View(types);
        }

        // GET: /Admin/RoomType/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/RoomType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                _roomTypeService.CreateRoomType(roomType);
                return RedirectToAction("Index");
            }
            return View(roomType);
        }

        // GET: /Admin/RoomType/Edit/5
        public IActionResult Edit(int id)
        {
            var type = _roomTypeService.GetRoomTypeById(id);
            if (type == null)
                return NotFound();

            return View(type);
        }

        // POST: /Admin/RoomType/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                _roomTypeService.UpdateRoomType(roomType);
                return RedirectToAction("Index");
            }
            return View(roomType);
        }

        // GET: /Admin/RoomType/Delete/5
        public IActionResult Delete(int id)
        {
            var type = _roomTypeService.GetRoomTypeById(id);
            if (type == null)
                return NotFound();

            return View(type);
        }

        // POST: /Admin/RoomType/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _roomTypeService.DeleteRoomType(id);
            return RedirectToAction("Index");
        }
    }
}
