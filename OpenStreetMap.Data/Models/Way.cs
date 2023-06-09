using OpenStreetMap.API.Models;
using SQLite;

namespace OpenStreetMap.Data.Models
{
    [Table(TableName)]
    public class Way
    {
        public const string TableName = "Ways";

        [PrimaryKey]
        public long Id { get; set; }

        public WayRank? Rank { get; set; }

        public string? Name { get; set; }

        /// <summary>
        /// Calculated by getting the absolute angle between every 3 Nodes, then averaging them.<br/>
        /// In this context                 <br/>
        /// - 0 degree is a straight line   <br/>
        /// - 180 degree is a hairpin turn  <br/>
        /// - Angles outside that range can not happen
        /// </summary>
        public float AverageAngle { get; set; }
    }
}
