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

        private MapDownloadJob? MapDownloadJob = null;
        private JobStatus? JobStatus = null;

        public void StartDownload(Area boundingBox, WayRank wayRanks)
        {
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
        }

        private void MapDownloadJob_OnDownloadComplete(object? sender, JobStatus e)
        {
            this.JobStatus = e;
            this.MapDownloadJob = null;
        }
    }
}
