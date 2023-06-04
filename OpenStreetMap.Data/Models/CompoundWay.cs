using SQLite;

namespace OpenStreetMap.Data.Models
{
    [Table(TableName)]
    public class CompoundWay
    {
        const string TableName = "CompoundWays";

        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }
    }
}
