using Funtlas.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Funtlas.UI.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var model = new HomeModel();
            model.Points = new (float, float)[]
            {
                (45.51f, -122.68f),
                //(37.77f, -122.43f),
                (34.04f, -118.2f)
            };

            return View(model);
        }
    }
}
