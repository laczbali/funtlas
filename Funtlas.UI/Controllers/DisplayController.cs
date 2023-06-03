using Funtlas.UI.Models.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Funtlas.UI.Controllers
{
    public class DisplayController : Controller
    {
        [HttpPost]
        public IActionResult RegionSelect([FromBody] RegionSelect selector)
        {
            return Ok();
        }
    }
}
