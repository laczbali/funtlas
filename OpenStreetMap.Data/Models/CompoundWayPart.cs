using SQLite;

namespace OpenStreetMap.Data.Models
{
    [Table(TableName)]
    public class CompoundWayPart
    {
        const string TableName = "CompoundWayParts";

        [PrimaryKey]
        public string Id { get; set; }

        private long compoundWayId;
        public long CompoundWayId
        {
            get => this.compoundWayId;
            set
            {
                this.compoundWayId = value;
                this.Id = $"{this.compoundWayId}@{this.wayId}";
            }
        }

        private long wayId;
        public long WayId
        {
            get => this.wayId;
            set
            {
                this.wayId = value;
                this.Id = $"{this.compoundWayId}@{this.wayId}";
            }
        }

        public int Order { get; set; }
    }
}
