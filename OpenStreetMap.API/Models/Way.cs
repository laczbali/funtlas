namespace OpenStreetMap.API.Models
{
    public class Way
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public long[] Nodes { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public string[] GetTagNames() => Tags?.Keys.ToArray() ?? new string[0];

        public bool HasTag(string tagName) => Tags?.ContainsKey(tagName) ?? false;

        public string? GetTagValue(string tagName) => Tags?.GetValueOrDefault(tagName);
    }
}
