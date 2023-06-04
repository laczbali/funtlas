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

        /// <summary>
        /// Quality 0 -> RED 255, GREEN 0 <br/>
        /// Quality 100 -> RED 0, GREEN 255 <br/>
        /// Quality 50 -> RED 127, GREEN 127 <br/>
        /// BLUE is always 0 <br/>
        /// <br/>
        /// If quality is greater than 100 it will be set to 100,
        /// if quality is less than 0 it will be set to 0
        /// </summary>
        /// <param name="quality"></param>
        public void SetQuality(int quality)
        {
            if (quality > 100)
            {
                quality = 100;
            }
            else if (quality < 0)
            {
                quality = 0;
            }

            R = (int)(255 - (quality * 2.55));
            G = (int)(quality * 2.55);
            B = 0;
        }

        /// <summary>
        /// Value will be returned as a hex string in the format #RRGGBB
        /// </summary>
        /// <returns></returns>
        public string ToHexString()
        {
            return $"#{R:X2}{G:X2}{B:X2}";
        }
    }
}
