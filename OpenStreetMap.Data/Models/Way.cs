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

        public int? MaxSpeed { get; set; }

        public string? Surface { get; set; }
    }
}
