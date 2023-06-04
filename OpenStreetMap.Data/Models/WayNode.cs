using SQLite;

namespace OpenStreetMap.Data.Models
{
    [Table(TableName)]
    public class WayNode
    {
        public const string TableName = "WayNodes";

        [PrimaryKey]
        public string Id { get; set; }

        private long wayId;
        public long WayId
        {
            get => this.wayId;
            set
            {
                this.wayId = value;
                this.Id = $"{this.wayId}@{this.nodeId}";
            }
        }

        private long nodeId;
        public long NodeId
        {
            get => this.nodeId;
            set
            {
                this.nodeId = value;
                this.Id = $"{this.wayId}@{this.nodeId}";
            }
        }

        public int Order { get; set; }

        public bool IsEndNode { get; set; }
    }
}
