using Funtlas.UI.Models.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Funtlas.UI.Controllers
{
    public class DisplayController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> RegionSelect([FromBody] RegionSelect selector)
        {
            return RedirectToAction("action", "controller", selector);
        }
    }
}
