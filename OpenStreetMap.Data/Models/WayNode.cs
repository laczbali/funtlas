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

        /// <summary>
        /// First or last node of the way
        /// </summary>
        public bool IsEndNode { get; set; }

        /// <summary>
        /// If true, the way is a crossroad <br/>
        /// - IsEndNode && has more than 2 connecting ways <br/>
        /// - !IsEndNode && has more than 1 coonecting way <br/>
        /// All connecting ways are taken into account, even if they are not in the DB
        /// due to wayrank or bounding box filtering
        /// </summary>
        public bool IsCrossRoad { get; set; }
    }
}
