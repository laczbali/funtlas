using Microsoft.AspNetCore.Mvc;
using OpenStreetMap.Data.Download;

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
            return View();
        }

        [HttpGet]
        public IActionResult Status()
        {
            return View();
        }

        [HttpGet]
        public IActionResult StatusUpdate()
        {
            var status = this.mapDownloadProvider.GetJobStatus();
            return Ok(status);
        }
    }
}
