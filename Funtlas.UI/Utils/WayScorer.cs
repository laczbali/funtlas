using OpenStreetMap.Data.DTOs;

namespace Funtlas.UI.Utils
{
	public class WayScorer
	{
		private readonly List<CompoundWayData> ways;
		private Dictionary<long, float> scores = new Dictionary<long, float>();

		public WayScorer(List<CompoundWayData> ways)
		{
			this.ways = ways;


		}

		public float GetScore(CompoundWayData way)
		{
			// return scores[way.Id];
			return 0;
		}
	}
}
