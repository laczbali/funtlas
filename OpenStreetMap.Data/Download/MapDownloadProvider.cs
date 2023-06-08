using OpenStreetMap.API;
using OpenStreetMap.API.Models;

namespace OpenStreetMap.Data.Download
{
    public class MapDownloadProvider
    {
        private readonly OverpassAPI overpassAPI;

        public MapDownloadProvider(OverpassAPI overpassAPI)
        {
            this.overpassAPI = overpassAPI;
        }

        public event EventHandler<JobStatus>? OnDownloadProgress;
        public event EventHandler<JobStatus>? OnDownloadComplete;

        private TaskCompletionSource JobAwaiterSource = new TaskCompletionSource();
        public Task JobTask => JobAwaiterSource.Task;

        private MapDownloadJob? MapDownloadJob = null;
        private JobStatus? JobStatus = null;

        public void StartDownload(Area boundingBox, WayRank wayRanks)
        {
            JobAwaiterSource = new TaskCompletionSource();
            JobStatus = new JobStatus();
            MapDownloadJob = new MapDownloadJob(overpassAPI);

            MapDownloadJob.BoundingBox = boundingBox;
            MapDownloadJob.WayRanks = wayRanks;
            MapDownloadJob.OnDownloadProgress += MapDownloadJob_OnDownloadProgress;
            MapDownloadJob.OnDownloadComplete += MapDownloadJob_OnDownloadComplete;

            _ = Task.Run(async () =>
            {
                await MapDownloadJob.StartJob();
            });
        }

        public bool IsJobRunning() => JobStatus?.IsRunning ?? false;

        public JobStatus? GetJobStatus() => JobStatus;

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
