namespace OpenStreetMap.API.Models
{
    public class Way
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public long[] Nodes { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
