using OpenStreetMap.API;
using OpenStreetMap.API.Models;

namespace OpenStreetMap.Data
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
        public TaskCompletionSource JobAwaiter = new TaskCompletionSource();

        private MapDownloadJob? MapDownloadJob = null;
        private JobStatus? JobStatus = null;

        public void StartDownload(Area boundingBox, WayRank wayRanks)
        {
            this.JobAwaiter = new TaskCompletionSource();
            this.JobStatus = new JobStatus();
            this.MapDownloadJob = new MapDownloadJob(this.overpassAPI);

            this.MapDownloadJob.BoundingBox = boundingBox;
            this.MapDownloadJob.WayRanks = wayRanks;
            this.MapDownloadJob.OnDownloadProgress += this.MapDownloadJob_OnDownloadProgress;
            this.MapDownloadJob.OnDownloadComplete += this.MapDownloadJob_OnDownloadComplete;

            _ = Task.Run(async () =>
            {
                await this.MapDownloadJob.StartJob();
            });
        }

        public bool IsJobRunning() => this.JobStatus?.IsRunning ?? false;

        public JobStatus? GetJobStatus() => this.JobStatus;

        private void MapDownloadJob_OnDownloadProgress(object? sender, JobStatus e)
        {
            this.JobStatus = e;
            this.OnDownloadProgress?.Invoke(this, e);
        }

        private void MapDownloadJob_OnDownloadComplete(object? sender, JobStatus e)
        {
            this.JobStatus = e;
            this.MapDownloadJob = null;

            this.OnDownloadComplete?.Invoke(this, e);
            this.JobAwaiter.SetResult();
        }
    }
}
