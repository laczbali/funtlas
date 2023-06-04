using Microsoft.AspNetCore.Mvc;
using OpenStreetMap.Data;

namespace Funtlas.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly MapDownloadProvider mapDownloadProvider;

        public HomeController(MapDownloadProvider mapDownloadProvider)
        {
            this.mapDownloadProvider = mapDownloadProvider;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Display/RegionSelect.cshtml");
        }

        [HttpGet]
        public IActionResult Status()
        {
            return View();
        }

        [HttpGet]
        public IActionResult StatusUpdate()
        {
            return Ok(this.mapDownloadProvider.GetJobStatus());
        }
    }
}
