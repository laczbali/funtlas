namespace OpenStreetMap.API.Models
{
    public class Node
    {
        public long Id { get; set; }
        public float? Lat { get; set; }
        public float? Lon { get; set; }

        public bool IsInArea(Area area)
        {
            if (this.Lat is null || this.Lon is null)
                throw new ArgumentException("Node has no coordinates");

            if (Lat < area.LatSouth || Lat > area.LatNorth)
                return false;

            if (Lon < area.LonWest || Lon > area.LonEast)
                return false;

            return true;
        }
    }
}
