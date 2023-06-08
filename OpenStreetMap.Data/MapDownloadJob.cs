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
        internal string DbPath;
        internal string DbName;
        internal string DbFullPath => Path.Combine(this.DbPath, this.DbName);

        private readonly OverpassAPI overpassAPI;

        internal MapDownloadJob(OverpassAPI overpassAPI)
        {
            this.overpassAPI = overpassAPI;

            this.DbPath = FileSystemUtil.EnsureDir(Path.Combine(Directory.GetCurrentDirectory(), "maps"));
            this.DbName = $"{DateTime.Now.Ticks}.db3";
        }

        internal async Task StartJob()
        {
            try
            {
                await this.InitDb();

                await this.DownloadRawData();

                await this.DownloadExtraNodeData();

                await this.BuildCompoundRoads();

                await this.GradeRoads();

                await this.SaveMapData();

                ReportComplete("Done");
            }
            catch (Exception e)
            {
                ReportError(e, "Execution failed");
            }
        }

        /// <summary>
        /// Creates the necessary tables in the DB
        /// </summary>
        /// <returns></returns>
        private async Task InitDb()
        {
            await DbUtil.UsingDbAsync(this.DbFullPath, async (db) =>
            {
                var tables = new List<Type>()
                {
                    typeof(Models.CompoundWay),
                    typeof(Models.CompoundWayPart),
                    typeof(Models.MapData),
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

        /// <summary>
        /// Downloads the raw data from the Overpass API and saves it to the database <br/>
        /// - <see cref="Models.Way"/>      <br/>
        /// - <see cref="Models.Node"/>     <br/>
        /// - <see cref="Models.WayNode"/>  <br/>
        /// - <see cref="Models.WayTag"/>   <br/>
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Adds additional data to nodes in db <br/>
        /// - <see cref="Models.Node"/> Lat\Lon         <br/>
        /// - <see cref="Models.WayNode"/> IsCrossroad  <br/>
        /// </summary>
        /// <returns></returns>
        private async Task DownloadExtraNodeData()
        {
            const int ChunkSize = 50;

            async Task ProcessEntityChunk<Tentity>(
                string reportText,
                string dbQuery,
                Func<Tentity[], Task<List<Tentity>>> processChunkFunc)
                where Tentity : new()
            {
                ReportProgress(reportText);

                // get all raw data
                var allEntities = await DbUtil.UsingDbAsync(this.DbFullPath, async (db) =>
                {
                    return await db.QueryAsync<Tentity>(dbQuery);
                });

                // split it into chunks, so we don't make too large requests
                var entityChunks = allEntities.Chunk(ChunkSize).ToList();
                int entityIndex = 0;

                foreach (var entityChunk in entityChunks)
                {
                    // call the processing function, save the updated data to db
                    var updatedEntities = await processChunkFunc(entityChunk);

                    await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
                    {
                        await db.UpdateAllAsync(updatedEntities);
                    });

                    entityIndex++;
                    ReportProgress(reportText, (int)((double)entityIndex / entityChunks.Count * 100));
                }
            }

            // download Lat\Lon data
            await ProcessEntityChunk<Models.Node>(
                "Downloading Node Lat-Lon data",
                SqlQueries.GetAllNodes,
                async (chunk) =>
                {
                    var nodeIds = chunk.Select(x => x.Id).ToList();
                    var nodeData = await this.overpassAPI.GetNodesById(nodeIds);
                    return nodeData.Select(x => new Models.Node
                    {
                        Id = x.Id,
                        Lat = x.Lat,
                        Lon = x.Lon
                    }).ToList();
                });

            // check if a node is a crossroads or not
            // if a WayNode is not an end node, it can only appear under one way
            // if a WayNode is an end node, it can only appear under two ways
            // otherwise a WayNode is a crossroads
            await ProcessEntityChunk<Models.WayNode>(
                "Downloading Node crossroad info",
                SqlQueries.GetAllWayNodes,
                async (chunk) =>
                {
                    var nodeIds = chunk.Select(x => x.NodeId).Distinct().ToList();
                    var wayData = await this.overpassAPI.GetWaysOfNodes(nodeIds);

                    bool IsCrossroad(long nodeId, bool isEndNode)
                    {
                        var connectingWayCount = wayData.Where(wd => wd.Nodes.Contains(nodeId)).Count();
                        return isEndNode ? connectingWayCount > 2 : connectingWayCount > 1;
                    }

                    return chunk.Select(x => new Models.WayNode
                    {
                        WayId = x.WayId,
                        NodeId = x.NodeId,
                        Order = x.Order,
                        IsEndNode = x.IsEndNode,
                        IsCrossRoad = IsCrossroad(x.NodeId, x.IsEndNode)
                    }).ToList();
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task BuildCompoundRoads()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task GradeRoads()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task SaveMapData()
        {
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

        public override string ToString()
        {
            var timeString = DateTime.Now.ToString("HH:mm");
            var progressString = Progress?.ToString("00") ?? "--";

            if (Error is null)
            {
                var baseString = $"{timeString}\t{progressString}%\t{Message}";
                return IsRunning ? baseString : $"{timeString}\tDONE";
            }
            else
            {
                var errorString = $"{Error.Message}\n{Error.StackTrace}";
                return $"{timeString}\tERROR\t{Message} @ {progressString}\n{errorString}";
            }
        }
    }
}
