using SQLite;

namespace OpenStreetMap.Data.Models
{
    [Table(TableName)]
    public class MapData
    {
        internal const string TableName = "MapData";

        [PrimaryKey]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
