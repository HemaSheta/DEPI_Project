using Depi_Project.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers.Customer
{
    [Route("Customer/Room")]
    public class RoomController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IRoomTypeService _roomTypeService;

        public RoomController(IRoomService roomService, IRoomTypeService roomTypeService)
        {
            _roomService = roomService;
            _roomTypeService = roomTypeService;
        }

        // Matches: /Customer/Room  AND  /Customer/Room/Index
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            var rooms = _roomService.GetAvailableRooms();

            // ⭐ Explicit view path
            return View("~/Views/Customer/Room/Index.cshtml", rooms);
        }

        // Matches: /Customer/Room/Details/5
        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            var room = _roomService.GetRoomById(id);
            if (room == null) return NotFound();

            // ⭐ Explicit view path
            return View("~/Views/Customer/Room/Details.cshtml", room);
        }
    }
}
