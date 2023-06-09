using MathNet.Spatial.Euclidean;
using SQLite;

namespace OpenStreetMap.Data.Models
{
    [Table(TableName)]
    public class Node
    {
        public const string TableName = "Nodes";

        [PrimaryKey]
        public long Id { get; set; }

        public float? Lat { get; set; }

        public float? Lon { get; set; }

        public Point2D ToPoint2D()
        {
            return new Point2D(Lon ?? 0, Lat ?? 0);
        }

        /// <summary>
        /// The three coordinates in the three nodes describe two lines             <br/><br/>
        /// This method will return the absolute angle between those two lines, as  <br/>
        /// - 0: the lines are parallel         <br/>
        /// - 90: the lines are perpendicular   <br/>
        /// - 179: the lines form a "hairpin"   <br/><br/>
        /// The lines are:          <br/>
        /// - 1: first -> middle    <br/>
        /// - 2: middle -> third    <br/>
        /// </summary>
        /// <returns></returns>
        public static float GetAngleBetween(Node first, Node middle, Node third)
        {
            var nodes = new[] { first, middle, third };
            if (nodes.Any(n => n is null))
            {
                throw new ArgumentNullException("All nodes must be non-null");
            }
            if (nodes.Any(n => n.Lat is null || n.Lon is null))
            {
                throw new ArgumentNullException("All nodes must have non-null latitudes and longitudes");
            }

            var line1 = new Line2D(first.ToPoint2D(), middle.ToPoint2D());
            var line2 = new Line2D(middle.ToPoint2D(), third.ToPoint2D());

            var angle = line1.Direction.AngleTo(line2.Direction).Degrees;
            if (angle == 180) angle = 0;
            return (float)angle;
        }
    }
}
