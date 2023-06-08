using OpenStreetMap.API;
using OpenStreetMap.API.Models;
using OpenStreetMap.Data;

var overpassApi = new OverpassAPI();
var mapDownloadProvider = new MapDownloadProvider(overpassApi);

var testArea = new Area
{
    LatNorth = 48.07083f,
    LatSouth = 48.06372f,
    LonEast = 20.63521f,
    LonWest = 20.62869f
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

await mapDownloadProvider.JobAwaiter.Task;

Console.WriteLine("\n\nSANDBOX DONE");
