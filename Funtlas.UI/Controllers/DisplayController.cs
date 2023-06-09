﻿using Funtlas.UI.Models.Controller;
using Microsoft.AspNetCore.Mvc;
using OpenStreetMap.API.Models;
using OpenStreetMap.Data;

namespace Funtlas.UI.Controllers
{
    public class DisplayController : Controller
    {
        private readonly MapDataProvider mapDownloadProvider;

        public DisplayController(MapDataProvider mapDownloadProvider)
        {
            this.mapDownloadProvider = mapDownloadProvider;
        }

        [HttpGet]
        public IActionResult RegionSelect()
        {
            return View("~/Views/Display/RegionSelect.cshtml");
        }

        [HttpPost]
        public IActionResult RegionSelect([FromBody] RegionSelect selector)
        {
            var boundingBox = new Area
            {
                LatNorth = selector.Bounds.Max(p => p.Lat),
                LatSouth = selector.Bounds.Min(p => p.Lat),
                LonEast = selector.Bounds.Max(p => p.Lon),
                LonWest = selector.Bounds.Min(p => p.Lon)
            };
            this.mapDownloadProvider.StartDownload(boundingBox, WayRank.PRIMARY | WayRank.SECONDARY | WayRank.TERTIARY);

            return RedirectToAction("Status", "Home");
        }
    }
}
