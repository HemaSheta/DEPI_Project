using Microsoft.AspNetCore.Mvc;
using Depi_Project.Services.Interfaces;
using Depi_Project.Models;

namespace Depi_Project.Controllers.Customer
{
    [Route("Customer/Cart")]
    public class CartController : Controller
    {
        private readonly IRoomService _roomService;

        public CartController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        // ---------------------
        // GET /Customer/Cart
        // ---------------------
        [HttpGet("")]
        public IActionResult Index()
        {
            var cart = GetCart();
            return View("~/Views/Customer/Cart/Index.cshtml", cart);
        }

        // ---------------------
        // POST /Customer/Cart/Add/5
        // ---------------------
        [HttpPost("Add/{roomId}")]
        public IActionResult Add(int roomId)
        {
            var room = _roomService.GetRoomById(roomId);
            if (room == null)
                return NotFound();

            var cart = GetCart();

            if (!cart.Any(c => c.RoomId == roomId))
                cart.Add(room);

            SaveCart(cart);

            return Json(new { success = true, message = "Added to cart!" });
        }

        // ---------------------
        // POST /Customer/Cart/Remove/5
        // ---------------------
        [HttpPost("Remove/{roomId}")]
        public IActionResult Remove(int roomId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.RoomId == roomId);

            if (item != null)
                cart.Remove(item);

            SaveCart(cart);

            return Json(new { success = true });
        }

        // ---------------------
        // GET /Customer/Cart/Count
        // ---------------------
        [HttpGet("Count")]
        public IActionResult Count()
        {
            var cart = GetCart();
            return Json(new { count = cart.Count });
        }


        // =============================
        // SESSION HELPERS
        // =============================
        private List<Room> GetCart()
        {
            var json = HttpContext.Session.GetString("CART");
            if (json == null) return new List<Room>();

            return System.Text.Json.JsonSerializer.Deserialize<List<Room>>(json);
        }

        private void SaveCart(List<Room> cart)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("CART", json);
        }
    }
}
