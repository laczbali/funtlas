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
    }
}
