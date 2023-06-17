using Funtlas.UI.Models.Base;
using Funtlas.UI.Models.View;
using Funtlas.UI.Utils;
using Microsoft.AspNetCore.Mvc;
using OpenStreetMap.Data;

namespace Funtlas.UI.Controllers
{
	public class HomeController : Controller
	{
		private readonly MapDataProvider mapDownloadProvider;

		public HomeController(MapDataProvider mapDownloadProvider)
		{
			this.mapDownloadProvider = mapDownloadProvider;
		}

		[HttpGet]
		public IActionResult Index()
		{
			var model = new HomePage
			{
				Maps = this.mapDownloadProvider.GetMaps(),
			};
			return View(model);
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

			if (status?.Error is not null)
			{
				// can't serialize complex exceptions
				status.Error = new Exception(status.Error.Message + Environment.NewLine + status.Error.StackTrace);
			}

			return Ok(status);
		}

		[HttpGet]
		public async Task<IActionResult> OpenMap(string mapName)
		{
			var mapData = await this.mapDownloadProvider.GetMapData(mapName);
			var scorer = new WayScorer(mapData);

			var model = new WaysDisplay();
			foreach (var cw in mapData)
			{
				var score = scorer.GetScore(cw) * 100;

				var color = new Color();
				color.SetQuality((int)score);

				model.Ways.Add(new Polyline
				{
					Points = cw.Nodes.Select(n => new Point((float)n.Lat!, (float)n.Lon!)).ToList(),
					Color = color
				});
			}

			return View("~/Views/Display/WaysDisplay.cshtml", model);
		}
	}
}
