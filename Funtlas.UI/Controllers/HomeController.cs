using Microsoft.AspNetCore.Mvc;

namespace Funtlas.UI.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Display/RegionSelect.cshtml");
        }
    }
}
