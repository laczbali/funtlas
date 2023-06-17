using OpenStreetMap.Data.Models;

namespace OpenStreetMap.Data.DTOs
{
	public class CompoundWayData
	{
		public long Id { get; set; }

		public int NodeCount { get; set; }

		public float AvgAngle { get; set; }

		public int CrossingCount { get; set; }

		[SQLite.Ignore]
		public List<Node> Nodes { get; set; } = new List<Node>();
	}
}
