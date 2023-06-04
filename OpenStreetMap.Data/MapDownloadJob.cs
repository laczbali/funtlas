using OpenStreetMap.API;
using OpenStreetMap.API.Models;

namespace OpenStreetMap.Data
{
    internal class MapDownloadJob
    {
        internal event EventHandler<JobStatus>? OnDownloadProgress;
        internal event EventHandler<JobStatus>? OnDownloadComplete;

        internal Area? BoundingBox;
        internal WayRank? WayRanks;
        internal string DbPath = Directory.GetCurrentDirectory();
        internal string DbName = $"{DateTime.Now.Ticks}.db3";
        internal string DbFullPath => Path.Combine(this.DbPath, this.DbName);

        private readonly OverpassAPI overpassAPI;

        internal MapDownloadJob(OverpassAPI overpassAPI)
        {
            this.overpassAPI = overpassAPI;
        }

        internal async Task StartJob()
        {
            this.InitDb();

            await this.DownloadRawData();

            ReportComplete("Done");
        }

        private void InitDb()
        {

        }

        private async Task DownloadRawData()
        {
            if (this.BoundingBox is null || this.WayRanks is null)
            {
                throw new InvalidOperationException("BoundingBox and WayRanks must be set before starting the job.");
            }

            // download raw way data
            ReportProgress("Downloading ways");
            List<Way> rawWayData = new List<Way>();
            try
            {
                rawWayData = await this.overpassAPI.GetWaysOfArea(this.BoundingBox, (WayRank)this.WayRanks);
            }
            catch (Exception e)
            {
                ReportError(e, "Error downloading initial way data");
            }

            // map raw way data to db model
            ReportProgress("Mapping ways to DB models", 0);
            List<Models.Way> ways = new List<Models.Way>();
            List<Models.Node> nodes = new List<Models.Node>();
            List<Models.WayNode> wayNodes = new List<Models.WayNode>();
            List<Models.WayTag> wayTags = new List<Models.WayTag>();

            foreach (var w in rawWayData)
            {
                ways.Add(new Models.Way
                {
                    Id = w.Id,
                    MaxSpeed = w.GetTag<int?>("maxspeed"),
                    Name = w.GetTag<string?>("name"),
                    Surface = w.GetTag<string?>("surface"),
                    Rank = w.GetTag<WayRank?>("highway")
                });

                nodes.AddRange(w.Nodes.Select(n => new Models.Node
                {
                    Id = n
                }));

                var wn = w.Nodes.Select((n, i) => new Models.WayNode
                {
                    WayId = w.Id,
                    NodeId = n,
                    Order = i
                });
                wn.First().IsEndNode = true;
                wn.Last().IsEndNode = true;
                wayNodes.AddRange(wn);

                wayTags.AddRange(w.Tags.Select(t => new Models.WayTag
                {
                    WayId = w.Id,
                    Key = t.Key,
                    Value = t.Value
                }));

                ReportProgress("Mapping ways to DB models", (int)((double)ways.Count / rawWayData.Count * 100));
            }

            // save to db
            nodes = nodes.DistinctBy(x => x.Id).ToList();

        }

        private void ReportProgress(string action, int? progress = null)
        {
            this.OnDownloadProgress?.Invoke(this, new JobStatus
            {
                Message = action,
                Progress = progress
            });
        }

        private void ReportComplete(string message)
        {
            this.OnDownloadComplete?.Invoke(this, new JobStatus
            {
                Message = message,
                IsRunning = false
            });
        }

        private void ReportError(Exception error, string? message = null)
        {
            this.OnDownloadComplete?.Invoke(this, new JobStatus
            {
                Message = message,
                Error = error,
                IsRunning = false
            });
        }
    }

    public class JobStatus
    {
        public string? Message { get; set; } = null;
        public int? Progress { get; set; } = null;
        public Exception? Error { get; set; } = null;
        public bool IsRunning { get; set; } = true;
    }
}
