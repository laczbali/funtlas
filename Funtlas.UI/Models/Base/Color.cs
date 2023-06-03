namespace Funtlas.UI.Models.Base
{
    public class Color
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public Color()
        {
            var rnd = new Random();
            R = rnd.Next(0, 255);
            G = rnd.Next(0, 255);
            B = rnd.Next(0, 255);
        }

        public Color(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public string ToHexString()
        {
            return $"#{R:X2}{G:X2}{B:X2}";
        }
    }
}
