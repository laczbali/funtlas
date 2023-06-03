using Funtlas.UI.Models.Base;

namespace Funtlas.UI.Models.View
{
    public class WaysDisplay
    {
        public Polyline[] Ways { get; set; }

        public Point[] GetBounds()
        {
            var corner1 = new Point
            {
                Lat = Ways.Min(p => p.Points.Min(p => p.Lat)),
                Lon = Ways.Min(p => p.Points.Min(p => p.Lon))
            };
            var corner2 = new Point
            {
                Lat = Ways.Max(p => p.Points.Max(p => p.Lat)),
                Lon = Ways.Max(p => p.Points.Max(p => p.Lon))
            };
            return new Point[] { corner1, corner2 };
        }
    }
}
