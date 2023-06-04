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
    Console.WriteLine($"{e.Progress?.ToString("00") ?? "--"}%\t{e.Message}");
};
mapDownloadProvider.OnDownloadComplete += (sender, e) =>
{
    Console.WriteLine($"Finished" + (e.Error is null ? string.Empty : $"\nERROR: {e.Error.Message}\n{e.Error.StackTrace}"));
};

await mapDownloadProvider.JobAwaiter.Task;

Console.WriteLine("Done");
