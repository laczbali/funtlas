using Blaczko.Core.Utils;
using Newtonsoft.Json;
using OpenStreetMap.API;
using OpenStreetMap.API.Models;

namespace OpenStreetMap.Data.Download
{
    internal class MapDownloadJob
    {
        // progress tracking
        internal event EventHandler<JobStatus>? OnDownloadProgress;
        internal event EventHandler<JobStatus>? OnDownloadComplete;

        // options
        internal Area? BoundingBox;
        internal WayRank? WayRanks;
        internal string DbPath;
        internal string DbName;
        internal string DbFullPath => Path.Combine(DbPath, DbName);
        internal bool DeleteDbOnFail = true;

        // dependencies
        private readonly OverpassApiProvider overpassAPI;

        internal MapDownloadJob(OverpassApiProvider overpassAPI)
        {
            this.overpassAPI = overpassAPI;

            DbPath = FileSystemUtil.EnsureDir(Path.Combine(Directory.GetCurrentDirectory(), "maps"));
            DbName = $"{DateTime.Now.Ticks}.db3";
        }

        internal async Task StartJob()
        {
            try
            {
                await InitDb();

                await DownloadRawData();

                await DownloadExtraNodeData();

                await BuildCompoundWays();

                await CalculateAngles();

                await SaveMapData();

                ReportComplete("Done");
            }
            catch (Exception e)
            {
                if (DeleteDbOnFail)
                {
                    File.Delete(DbFullPath);
                }

                ReportError(e, "Execution failed");
            }
        }

        /// <summary>
        /// Creates the necessary tables and views in the DB
        /// </summary>
        /// <returns></returns>
        private async Task InitDb()
        {
            await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
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

                var views = new List<string>()
                {
                    ViewDefinitions.CompoundWayData
                };

                foreach (var v in views)
                {
                    await db.ExecuteAsync(v);
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
            if (BoundingBox is null || WayRanks is null)
            {
                throw new InvalidOperationException("BoundingBox and WayRanks must be set before starting the job.");
            }

            // download raw way data
            ReportProgress("Downloading raw way data");
            List<Way> rawWayData = new List<Way>();
            try
            {
                rawWayData = await overpassAPI.GetWaysOfArea(BoundingBox, (WayRank)WayRanks);
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
                    Name = w.GetTag<string?>("name"),
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
                    SortOrder = i
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

                ReportProgress("Mapping ways to DB models", ways.Count, rawWayData.Count);
            }

            // save to db
            nodes = nodes.DistinctBy(x => x.Id).ToList();
            await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
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
                var allEntities = await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
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
                    ReportProgress(reportText, entityIndex, entityChunks.Count);
                }
            }

            // download Lat\Lon data
            await ProcessEntityChunk<Models.Node>(
                "Downloading Node Lat-Lon data",
                SqlQueries.GetAllNodes,
                async (chunk) =>
                {
                    var nodeIds = chunk.Select(x => x.Id).ToList();
                    var nodeData = await overpassAPI.GetNodesById(nodeIds);
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
                    var remoteWayData = await overpassAPI.GetWaysOfNodes(nodeIds);

                    bool HasCrossing(long nodeId, bool isEndNode)
                    {
                        var connectingWayCount = remoteWayData.Where(wd => wd.Nodes.Contains(nodeId)).Count();
                        return isEndNode ? connectingWayCount > 2 : connectingWayCount > 1;
                    }

                    async Task<bool> IsCrossRoad(long nodeId, bool isEndNode)
                    {
                        var wayNodes = await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
                        {
                            return await db.QueryAsync<Models.WayNode>(SqlQueries.GetAllWayNodesOfNode, nodeId);
                        });
                        var connectingWayCount = wayNodes.Count();
                        return isEndNode ? connectingWayCount > 2 : connectingWayCount > 1;
                    }

                    var wayNodes = new List<Models.WayNode>();
                    foreach (var item in chunk)
                    {
                        var hasCrossing = HasCrossing(item.NodeId, item.IsEndNode);
                        var isCrossroad = await IsCrossRoad(item.NodeId, item.IsEndNode);

                        wayNodes.Add(new Models.WayNode
                        {
                            WayId = item.WayId,
                            NodeId = item.NodeId,
                            SortOrder = item.SortOrder,
                            IsEndNode = item.IsEndNode,
                            HasCrossing = hasCrossing,
                            IsCrossroad = isCrossroad
                        });
                    }

                    return wayNodes;
                });
        }

        /// <summary>
        /// Build longer, consecutive ways from smaller ways, where one ends where the other begins. <br/>
        /// Results will be <see cref="Models.CompoundWay"/> and <see cref="Models.CompoundWayNode"/>
        /// </summary>
        /// <returns></returns>
        private async Task BuildCompoundWays()
        {
            ReportProgress("Building compound ways");

            #region build compound ways

            var allEndWayNodes = await DbUtil.UsingDbAsync(this.DbFullPath, async (db) =>
            {
                return await db.QueryAsync<Models.WayNode>(SqlQueries.GetNonCrossEndWayNodes);
            });

            List<List<long>> compounds = allEndWayNodes.Select(x => new List<long> { x.WayId }).ToList();

            bool addedNew = false;
            do
            {
                addedNew = false;
                foreach (var compound in compounds)
                {
                    // basically, we are listing all the ways added to the collection (by end-nodes)
                    // and all the ways that are not added (also by end-nodes)
                    // if a not-added way begins, where an added one ends,
                    // there will be an "intersect" end-node between the two
                    // and we add the way that owns that end-node to the collection
                    // and by extension, we add its other ("farther") end-node

                    var haveEndWayNodes = allEndWayNodes.Where(x => compound.Contains(x.WayId));
                    var haveNodeIds = haveEndWayNodes.Select(x => x.NodeId).Distinct();

                    var notHaveEndWayNodes = allEndWayNodes.Except(haveEndWayNodes);
                    var toAddWayNodes = notHaveEndWayNodes.Where(x => haveNodeIds.Contains(x.NodeId));
                    var toAddWayIds = toAddWayNodes.Select(x => x.WayId);

                    if (toAddWayIds.Count() > 0)
                    {
                        compound.AddRange(toAddWayIds);
                        addedNew = true;
                    }
                }
            }
            while (addedNew);

            #endregion

            #region get rid of duplicates, build models

            compounds = compounds.DistinctBy(compounds =>
            {
                var distinct = compounds.Distinct();
                var ordered = distinct.OrderBy(x => x);
                var stringified = string.Join(',', ordered);
                return stringified ?? string.Empty;
            }).ToList();

            var newCompoundWays = new List<Models.CompoundWay>();
            var newCompoundWayParts = new List<Models.CompoundWayPart>();
            int compoundWayIndex = 1;
            foreach (var compound in compounds)
            {
                if (!compound.Any())
                {
                    continue;
                }

                newCompoundWays.Add(new Models.CompoundWay
                {
                    Id = compoundWayIndex
                });

                newCompoundWayParts.AddRange(compound.Distinct().Select(x => new Models.CompoundWayPart
                {
                    CompoundWayId = compoundWayIndex,
                    WayId = x,
                    SortOrder = -1
                }).ToList());

                compoundWayIndex++;
            }

            // save to db
            await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
            {
                await db.InsertAllAsync(newCompoundWays);
                await db.InsertAllAsync(newCompoundWayParts);
            });

            #endregion

            #region calculate sort orders

            foreach (var compWay in newCompoundWays)
            {
                var compWayId = compWay.Id;
                var borderNodeIds = (await DbUtil.UsingDbAsync(this.DbFullPath, async (db) =>
                {
                    return await db.QueryAsync<Models.WayNode>(SqlQueries.GetEndNodesIdsOfCompoundWay, compWayId);
                })).Select(x => x.NodeId);

                // debug
                if (!borderNodeIds.Any())
                {
                    Console.WriteLine();
                }

                var endNodeId = borderNodeIds.Last();
                var currentWayStartNodeId = borderNodeIds.First();
                var currentWayId = (await DbUtil.UsingDbAsync(this.DbFullPath, async (db) =>
                {
                    return await db.QueryAsync<Models.WayNode>(SqlQueries.GetAllWayNodesOfNode, currentWayStartNodeId);
                })).First().WayId;

                int sortOrder = 0;
                var reachedEnd = false;

                while (!reachedEnd)
                {
                    // set sort order for current way
                    newCompoundWayParts.First(cwp => cwp.WayId == currentWayId).SortOrder = sortOrder;
                    sortOrder++;

                    // get the other end of the current way
                    var wayNodes = await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
                    {
                        return await db.QueryAsync<Models.WayNode>(SqlQueries.GetAllWayNodesOfWay, currentWayId);
                    });
                    currentWayStartNodeId = wayNodes.First(wn => wn.NodeId != currentWayStartNodeId && wn.IsEndNode).NodeId;

                    // if the other end is the total end, we are done
                    if (currentWayStartNodeId == endNodeId)
                    {
                        reachedEnd = true;
                        continue;
                    }

                    // otherwise, get the next way
                    var wayNodesOfStartNode = await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
                    {
                        return await db.QueryAsync<Models.WayNode>(SqlQueries.GetAllWayNodesOfNode, currentWayStartNodeId);
                    });
                    currentWayId = wayNodesOfStartNode.First(wn => wn.WayId != currentWayId).WayId;
                }
            }

            await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
            {
                await db.UpdateAllAsync(newCompoundWayParts);
            });

            #endregion
        }

        /// <summary>
        /// Goes through all the ways in the DB, and caluclates the average angle between its nodes.
        /// The result will be saved to the DB.
        /// </summary>
        /// <returns></returns>
        private async Task CalculateAngles()
        {
            ReportProgress("Calulating road angles", 0);

            var allWays = await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
            {
                return await db.QueryAsync<Models.Way>(SqlQueries.GetAllWays);
            });

            int wayIndex = 0;
            foreach (var way in allWays)
            {
                var nodes = await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
                {
                    return await db.QueryAsync<Models.Node>(SqlQueries.GetAllNodesOfWay, way.Id);
                });

                if (nodes.Count < 3)
                {
                    way.AverageAngle = 0;
                    continue;
                }

                var angles = new List<float>();

                for (int i = 2; i < nodes.Count; i++)
                {
                    var node1 = nodes[i - 2];
                    var node2 = nodes[i - 1];
                    var node3 = nodes[i];
                    angles.Add(Models.Node.GetAngleBetween(node1, node2, node3));
                }

                way.AverageAngle = angles.Average();

                wayIndex++;
                ReportProgress("Calulating road angles", wayIndex, allWays.Count);
            }

            await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
            {
                await db.UpdateAllAsync(allWays);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task SaveMapData()
        {
            var data = new List<Models.MapData>
            {
                new Models.MapData
                {
                    Key = "BoundingBox",
                    Value = JsonConvert.SerializeObject(BoundingBox)
                }
            };

            await DbUtil.UsingDbAsync(DbFullPath, async (db) =>
            {
                await db.InsertAllAsync(data);
            });
        }

        private void ReportProgress(string action, int currentIndex, int maxIndex)
        {
            var progress = (int)((double)currentIndex / maxIndex * 100);
            ReportProgress(action, progress);
        }

        private void ReportProgress(string action, int? progress = null)
        {
            OnDownloadProgress?.Invoke(this, new JobStatus
            {
                Message = action,
                Progress = progress
            });
        }

        private void ReportComplete(string message)
        {
            OnDownloadComplete?.Invoke(this, new JobStatus
            {
                Message = message,
                IsRunning = false
            });
        }

        private void ReportError(Exception error, string? message = null)
        {
            OnDownloadComplete?.Invoke(this, new JobStatus
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
