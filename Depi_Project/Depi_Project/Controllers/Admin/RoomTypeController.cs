using Depi_Project.Models;
using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Admin
{
    [Authorize]
    [AdminOnly]
    [Route("Admin/RoomType")]
    public class RoomTypeController : Controller
    {
        private readonly IRoomTypeService _roomTypeService;

        public RoomTypeController(IRoomTypeService roomTypeService)
        {
            _roomTypeService = roomTypeService;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var types = _roomTypeService.GetAllRoomTypes();
            return View("~/Views/Admin/RoomType/Index.cshtml", types);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/RoomType/Create.cshtml");
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                _roomTypeService.CreateRoomType(roomType);
                return RedirectToAction("Index");
            }

            return View("~/Views/Admin/RoomType/Create.cshtml", roomType);
        }

        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var type = _roomTypeService.GetRoomTypeById(id);
            if (type == null)
                return NotFound();

            return View("~/Views/Admin/RoomType/Edit.cshtml", type);
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                _roomTypeService.UpdateRoomType(roomType);
                return RedirectToAction("Index");
            }

            return View("~/Views/Admin/RoomType/Edit.cshtml", roomType);
        }

        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var type = _roomTypeService.GetRoomTypeById(id);
            if (type == null)
                return NotFound();

            return View("~/Views/Admin/RoomType/Delete.cshtml", type);
        }

        [HttpPost("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _roomTypeService.DeleteRoomType(id);
            return RedirectToAction("Index");
        }
    }
}
