using OpenStreetMap.API;
using OpenStreetMap.API.Models;
using OpenStreetMap.Data.Download;

var overpassApi = new OverpassAPI();
var mapDownloadProvider = new MapDownloadProvider(overpassApi);

var testArea = new Area
{
    LatNorth = 48.08083f,
    LatSouth = 48.05372f,
    LonEast = 20.66521f,
    LonWest = 20.60869f
};

mapDownloadProvider.StartDownload(testArea, WayRank.PRIMARY | WayRank.SECONDARY);
mapDownloadProvider.OnDownloadProgress += (sender, e) =>
{
    Console.WriteLine(e);
};
mapDownloadProvider.OnDownloadComplete += (sender, e) =>
{
    Console.WriteLine(e);
};

await mapDownloadProvider.JobTask;

Console.WriteLine("\n\nSANDBOX DONE");
