// Controllers/Admin/RoomTypeController.cs
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
            var list = _roomTypeService.GetAllRoomTypes();
            return View("~/Views/Admin/RoomType/Index.cshtml", list);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/RoomType/Create.cshtml");
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RoomType model)
        {
            // basic model validation
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/RoomType/Create.cshtml", model);
            }

            // uniqueness check (case-insensitive)
            if (_roomTypeService.RoomTypeNameExists(model.RoomTypeName))
            {
                ModelState.AddModelError(nameof(model.RoomTypeName), "This room type name already exists.");
                return View("~/Views/Admin/RoomType/Create.cshtml", model);
            }

            _roomTypeService.CreateRoomType(model);
            TempData["Success"] = "Room type created.";
            return Redirect("/Admin/RoomType");
        }

        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            var rt = _roomTypeService.GetRoomTypeById(id);
            if (rt == null) return NotFound();
            return View("~/Views/Admin/RoomType/Edit.cshtml", rt);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, RoomType model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/RoomType/Edit.cshtml", model);
            }

            // uniqueness check excluding current id
            if (_roomTypeService.RoomTypeNameExists(model.RoomTypeName, id))
            {
                ModelState.AddModelError(nameof(model.RoomTypeName), "This room type name already exists.");
                return View("~/Views/Admin/RoomType/Edit.cshtml", model);
            }

            var exists = _roomTypeService.GetRoomTypeById(id);
            if (exists == null) return NotFound();

            exists.RoomTypeName = model.RoomTypeName;
            exists.Price = model.Price;
            exists.NumOfPeople = model.NumOfPeople;

            _roomTypeService.UpdateRoomType(exists);
            TempData["Success"] = "Room type updated.";
            return Redirect("/Admin/RoomType");
        }

        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var rt = _roomTypeService.GetRoomTypeById(id);
            if (rt == null) return NotFound();
            return View("~/Views/Admin/RoomType/Delete.cshtml", rt);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _roomTypeService.DeleteRoomType(id);
            TempData["Success"] = "Room type deleted.";
            return Redirect("/Admin/RoomType");
        }
    }
}
