using Blaczko.Core.Utils;
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
            try
            {
                await this.InitDb();

                await this.DownloadRawData();

                await this.DownloadExtraNodeData();

                ReportComplete("Done");
            }
            catch (Exception e)
            {
                ReportError(e, "Execution failed");
            }
        }

        private async Task InitDb()
        {
            await DbUtil.UsingDbAsync(this.DbFullPath, async (db) =>
            {
                var tables = new List<Type>()
                {
                    typeof(Models.CompoundWay),
                    typeof(Models.CompoundWayPart),
                    typeof(Models.Node),
                    typeof(Models.Way),
                    typeof(Models.WayNode),
                    typeof(Models.WayTag)
                };

                foreach (var t in tables)
                {
                    await db.CreateTableAsync(t);
                }
            });
        }

        private async Task DownloadRawData()
        {
            if (this.BoundingBox is null || this.WayRanks is null)
            {
                throw new InvalidOperationException("BoundingBox and WayRanks must be set before starting the job.");
            }

            // download raw way data
            ReportProgress("Downloading raw way data");
            List<Way> rawWayData = new List<Way>();
            try
            {
                rawWayData = await this.overpassAPI.GetWaysOfArea(this.BoundingBox, (WayRank)this.WayRanks);
            }
            catch (Exception e)
            {
                ReportError(e, "Error downloading raw way data");
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
                    Rank = w.GetRank()
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
                }).ToList();
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
            await DbUtil.UsingDbAsync(this.DbFullPath, async (db) =>
            {
                await db.InsertAllAsync(ways);
                await db.InsertAllAsync(nodes);
                await db.InsertAllAsync(wayNodes);
                await db.InsertAllAsync(wayTags);
            });
        }

        private async Task DownloadExtraNodeData()
        {
            // IsCrossroad, Lat\Lon 
            return;
        }

        private async Task BuildCompoundRoads()
        {
            return;
        }

        private async Task GradeRoads()
        {
            return;
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
