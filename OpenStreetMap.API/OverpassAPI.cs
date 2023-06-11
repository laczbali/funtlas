using OpenStreetMap.API.Models;

namespace OpenStreetMap.API
{
    // TODO: refactor this class as
    // - instead of a service, it should be instanciated with a bounding box
    // - in the constructor, it should download all data of the bounding box (remoteSource.DownloadAllData)
    //      - there should be an IsReady task completion source, that can be checked later
    // - results of that download should be stored in private fields
    // - the public methods should then just return the requested data from the cache

    public class OverpassAPI
    {
        private OverpassRemoteSource remoteSource;

        public OverpassAPI(OverpassApiConfig? config = null)
        {
            this.remoteSource = new OverpassRemoteSource(config ?? new OverpassApiConfig());
        }

        public async Task<List<Way>> GetWaysOfArea(Area boundingBox, WayRank wayRanks)
        {
            return await this.remoteSource.DownloadWaysOfArea(boundingBox, wayRanks);
        }

        public async Task<List<Way>> GetWaysOfNodes(IEnumerable<long> nodeIds)
        {
            return await this.remoteSource.DownloadWaysOfNodes(nodeIds);
        }

        public async Task<List<Node>> GetNodesById(IEnumerable<long> nodeIds)
        {
            return await this.remoteSource.DownloadNodesById(nodeIds);
        }
    }
}