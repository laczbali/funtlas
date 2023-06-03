using System.Globalization;

namespace Funtlas.UI.Models.Base
{
    public class Point
    {
        public float Lat { get; set; }
        public float Lon { get; set; }

        private readonly NumberFormatInfo nfi = new NumberFormatInfo
        {
            NumberDecimalSeparator = ".",
            NumberDecimalDigits = 4
        };

        public Point()
        {
        }

        public Point(float lat, float lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public string ToJsObjectString()
        {
            return $"{{\"lat\": {Lat.ToString(nfi)}, \"lon\": {Lon.ToString(nfi)}}}";
        }

        public string ToJsArrayString()
        {
            return $"[{Lat.ToString(nfi)}, {Lon.ToString(nfi)}]";
        }
    }
}
