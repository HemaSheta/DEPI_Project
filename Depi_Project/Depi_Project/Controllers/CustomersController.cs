using Microsoft.AspNetCore.Mvc;

namespace Depi_Project.Controllers
{
    public class CustomersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
