using SQLite;

namespace OpenStreetMap.Data.Models
{
    [Table(TableName)]
    public class WayTag
    {
        internal const string TableName = "WayTags";

        [PrimaryKey]
        public string Id { get; set; }

        private long wayId;
        public long WayId
        {
            get => this.wayId;
            set
            {
                this.wayId = value;
                this.Id = $"{this.wayId}@{this.key}";
            }
        }

        private string key;
        public string Key
        {
            get => this.key;
            set
            {
                this.key = value;
                this.Id = $"{this.wayId}@{this.key}";
            }
        }

        public string Value { get; set; }
    }
}
