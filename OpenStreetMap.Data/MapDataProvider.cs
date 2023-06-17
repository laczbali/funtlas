using Blaczko.Core.Utils;
using OpenStreetMap.API;
using OpenStreetMap.API.Models;
using OpenStreetMap.Data.DTOs;

namespace OpenStreetMap.Data
{
    public class MapDataProvider
    {
        private readonly OverpassApiProvider overpassAPI;

        public MapDataProvider(OverpassApiProvider overpassAPI)
        {
            this.overpassAPI = overpassAPI;

            DbLocation = FileSystemUtil.EnsureDir(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maps"));
        }

        public event EventHandler<JobStatus>? OnDownloadProgress;
        public event EventHandler<JobStatus>? OnDownloadComplete;

        private TaskCompletionSource JobAwaiterSource = new TaskCompletionSource();
        public Task JobTask => JobAwaiterSource.Task;

        public string DbLocation { get; private set; }

        private MapDownloadJob? MapDownloadJob = null;
        private JobStatus? JobStatus = null;

        public void StartDownload(Area boundingBox, WayRank wayRanks)
        {
            JobAwaiterSource = new TaskCompletionSource();
            JobStatus = new JobStatus();
            MapDownloadJob = new MapDownloadJob(overpassAPI);

            MapDownloadJob.BoundingBox = boundingBox;
            MapDownloadJob.WayRanks = wayRanks;
            MapDownloadJob.DbPath = DbLocation;
            MapDownloadJob.OnDownloadProgress += MapDownloadJob_OnDownloadProgress;
            MapDownloadJob.OnDownloadComplete += MapDownloadJob_OnDownloadComplete;

            _ = Task.Run(async () =>
            {
                await MapDownloadJob.StartJob();
            });
        }

        public bool IsJobRunning() => JobStatus?.IsRunning ?? false;

        public JobStatus? GetJobStatus() => JobStatus;

        public List<string> GetMaps()
        {
            var mapFiles = Directory.GetFiles(DbLocation, "*.db3");
            return mapFiles.Select(x => Path.GetFileName(x)).ToList();
        }

        public async Task<List<CompoundWayData>> GetMapData(string dbName)
        {
            var mapFullPath = Path.Combine(DbLocation, dbName);
            if (!File.Exists(mapFullPath))
            {
                throw new ArgumentException($"Failed to find map DB at [{mapFullPath}]");
            }

            var wayData = await DbUtil.UsingDbAsync(mapFullPath, async (db) =>
            {
                return await db.QueryAsync<CompoundWayData>(SqlQueries.GetAllCompoundWayData);
            });

            foreach (var cw in wayData)
            {
                cw.Nodes = await DbUtil.UsingDbAsync(mapFullPath, async (db) =>
                {
                    return await db.QueryAsync<Models.Node>(SqlQueries.GetNodesOfCompoundWay);
                });
            }

            // the issue here, is that we have a sort order for
            // - ways in a compound way
            // - nodes in a way
            // the two don't necessarliy line up, so instead of
            //      A1-2-3 B1-2-3 C1-2-3
            // it might go like
            //      A1-2-3 B3-2-1 C1-2-3
            // so we need to add an "orientation" field to the CompoundWayParts,
            // calculate that value during the initial buildup,
            // and use it here

            return wayData;
        }

        private void MapDownloadJob_OnDownloadProgress(object? sender, JobStatus e)
        {
            JobStatus = e;
            OnDownloadProgress?.Invoke(this, e);
        }

        private void MapDownloadJob_OnDownloadComplete(object? sender, JobStatus e)
        {
            JobStatus = e;
            MapDownloadJob = null;

            OnDownloadComplete?.Invoke(this, e);
            JobAwaiterSource.SetResult();
        }
    }
}
