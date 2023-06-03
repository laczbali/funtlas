using Funtlas.UI.Models.Base;
using Funtlas.UI.Models.View;
using Microsoft.AspNetCore.Mvc;

namespace Funtlas.UI.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            var model = new WaysDisplay
            {
                Ways = new Polyline[]
                {
                    new Polyline
                    {
                        Color = new Color(255, 0, 0),
                        Points = new Point[]
                        {
                            new Point(45.51f, -122.68f),
                            new Point(37.77f, -122.43f),
                            new Point(34.04f, -118.2f)
                        }
                    },

                    new Polyline
                    {
                        Color = new Color(0, 255, 0),
                        Points = new Point[]
                        {
                            new Point(56.52f, -113.67f),
                            new Point(58.78f, -113.44f),
                            new Point(55.05f, -109.3f)
                        }
                    }
                }
            };

            return View("~/Views/Display/Ways.cshtml", model);
        }
    }
}
