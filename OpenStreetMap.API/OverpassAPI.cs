using Blaczko.Core.Wrappers;
using OpenStreetMap.API.Models;
using System.Text;

namespace OpenStreetMap.API
{
    public class OverpassAPI
    {
        private readonly OverpassApiConfig config;

        public OverpassAPI(OverpassApiConfig? config = null)
        {
            this.config = config ?? new OverpassApiConfig();
        }

        public async Task<List<Way>> GetWaysOfArea(Area boundingBox, WayRank wayRanks)
        {
            var filters = string.Empty;

            if (wayRanks.HasFlag(WayRank.MINOR))
            {
                filters += $"way({boundingBox});";
            }
            else
            {
                if (wayRanks.HasFlag(WayRank.PRIMARY)) filters += $"way({boundingBox})[highway=primary];" + Environment.NewLine;
                if (wayRanks.HasFlag(WayRank.SECONDARY)) filters += $"way({boundingBox})[highway=secondary];" + Environment.NewLine;
                if (wayRanks.HasFlag(WayRank.TERTIARY)) filters += $"way({boundingBox})[highway=tertiary];" + Environment.NewLine;
            }

            var query = $@"
			[out:json];
			(
  				(
					{filters}
				);
			);
			out;
			";

            return await RunQuery<Way>(query);
        }

        public async Task<List<Way>> GetWaysOfNodes(IEnumerable<long> nodeIds)
        {
            var query = $@"
            [out:json];
            (
              node(id:{string.Join(',', nodeIds)});
              way(bn) -> .ways;
            );
            (.ways;); out;
            ";

            return await RunQuery<Way>(query);
        }

        private async Task<List<T>> RunQuery<T>(string query)
        {
            var result = await HttpClientWrapper.MakeRequestAsync<Base<T>>(
                this.config.InterpreterUrl.AbsoluteUri,
                HttpMethod.Post,
                new StringContent(query, Encoding.UTF8, "application/x-www-form-urlencoded"));

            return result.Elements.ToList();
        }
    }
}