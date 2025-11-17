using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers
{
    public class AdminsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
