using Blaczko.Core.Wrappers;
using OpenStreetMap.API.Models;

namespace OpenStreetMap.API
{
	public class OverpassAPI
	{
		private readonly OverpassApiConfig config;

		public OverpassAPI(OverpassApiConfig? config = null)
		{
			this.config = config ?? new OverpassApiConfig();
		}

		public async Task GetWaysOfArea(Area boundingBox, WayRank wayRanks)
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

			return;
		}

		private async Task<T> RunQuery<T>(string query)
		{
			return HttpClientWrapper.MakeRequestAsync<T>(this.config.InterpreterUrl, HttpMethod.Post, query);
		}
	}
}