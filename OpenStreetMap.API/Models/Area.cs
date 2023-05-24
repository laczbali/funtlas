using System.Globalization;

namespace OpenStreetMap.API.Models
{
	public class Area
	{
		public float LatNorth { get; set; }
		public float LatSouth { get; set; }
		public float LonWest { get; set; }
		public float LonEast { get; set; }

		public override string ToString()
		{
			NumberFormatInfo nfi = new NumberFormatInfo
			{
				NumberDecimalSeparator = ".",
				NumberDecimalDigits = 4
			};

			return $"{LatSouth.ToString(nfi)}, {LonWest.ToString(nfi)}, {LatNorth.ToString(nfi)}, {LonEast.ToString(nfi)}";
		}
	}
}
