using Blaczko.Core.Wrappers;
using Newtonsoft.Json.Linq;
using OpenStreetMap.API.Models;
using System.Text;

namespace OpenStreetMap.API
{
    public class OverpassApi
    {
        private readonly OverpassApiConfig config;

        public OverpassApi(OverpassApiConfig config)
        {
            this.config = config;
        }

        public async Task<(List<Node> nodes, List<Way> selectedWays, List<Way> connectingWays)> DownloadAllDataOfArea(Area boundingBox, WayRank wayRanks, bool includeConnectingWays = true)
        {
            var selectedWayRanks = new List<string>();
            if (wayRanks.HasFlag(WayRank.PRIMARY)) selectedWayRanks.Add("primary");
            if (wayRanks.HasFlag(WayRank.SECONDARY)) selectedWayRanks.Add("secondary");
            if (wayRanks.HasFlag(WayRank.TERTIARY)) selectedWayRanks.Add("tertiary");

            var query = $@"
                [out:json];
                (
                    // filter by bounding box, road type
	                way({boundingBox})
  		                [highway~""^({string.Join('|', selectedWayRanks)})$""]
  	                -> .sel_area;

  	                // get nodes of initial selection
	                node(w.sel_area);
  
  	                // get connecting ways of selected nodes
  	                {(includeConnectingWays ? string.Empty : "//")} way(bn);
                );
                out;
            ";

            var result = await RunQuery<JObject>(query);

            var nodes = new List<Node>();
            var allWays = new List<Way>();

            foreach (var jo in result)
            {
                var joType = jo.GetValue("type")?.Value<string>();
                if (joType == "node")
                {
                    nodes.Add(jo.ToObject<Node>()!);
                }
                if (joType == "way")
                {
                    allWays.Add(jo.ToObject<Way>()!);
                }
            }

            allWays = allWays.DistinctBy(w => w.Id).ToList();

            if (!includeConnectingWays)
            {
                return (nodes, allWays, new List<Way>());
            }

            var selectedWays = allWays.Where(w =>
            {
                var isCorrectRank = selectedWayRanks.Contains(w.GetTag("highway") ?? string.Empty);

                var wayNodes = nodes.Where(n => w.Nodes.Contains(n.Id)).ToList();
                var isInBounds = wayNodes.Any(n => n.IsInArea(boundingBox));

                return isCorrectRank && isInBounds;
            }).ToList();

            var connectingWays = allWays.Except(selectedWays).ToList();

            return (nodes, selectedWays, connectingWays);
        }

        public async Task<List<Way>> DownloadWaysOfArea(Area boundingBox, WayRank wayRanks)
        {
            var filters = string.Empty;

            if (wayRanks.HasFlag(WayRank.PRIMARY)) filters += $"way({boundingBox})[highway=primary];" + Environment.NewLine;
            if (wayRanks.HasFlag(WayRank.SECONDARY)) filters += $"way({boundingBox})[highway=secondary];" + Environment.NewLine;
            if (wayRanks.HasFlag(WayRank.TERTIARY)) filters += $"way({boundingBox})[highway=tertiary];" + Environment.NewLine;

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

        public async Task<List<Way>> DownloadWaysOfNodes(IEnumerable<long> nodeIds)
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

        public async Task<List<Node>> DownloadNodesById(IEnumerable<long> nodeIds)
        {
            var query = $@"
            [out:json];
            (
              node(id:{string.Join(',', nodeIds)});
            );
            out;
            ";

            return await RunQuery<Node>(query);
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
