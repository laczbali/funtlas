namespace Funtlas.UI.Models.Base
{
    public class Polyline
    {
        public Point[] Points { get; set; }
        public Color Color { get; set; }

        public Point[] GetBounds()
        {
            var corner1 = new Point
            {
                Lat = Points.Min(p => p.Lat),
                Lon = Points.Min(p => p.Lon)
            };
            var corner2 = new Point
            {
                Lat = Points.Max(p => p.Lat),
                Lon = Points.Max(p => p.Lon)
            };
            return new Point[] { corner1, corner2 };
        }

        public string ToJsObjectString()
        {
            var color = $"\"color\": \"{Color.ToHexString()}\"";
            var points = $"\"points\": [{string.Join(",", Points.Select(p => p.ToJsObjectString()))}]";
            return $"{{{color},{points}}}";
        }
    }
}
