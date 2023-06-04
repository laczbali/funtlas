namespace OpenStreetMap.API.Models
{
    public class Way
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public List<long> Nodes { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public string? GetTag(string key)
        {
            if (this.Tags == null)
            {
                return null;
            }

            if (this.Tags.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        public T? GetTag<T>(string key)
        {
            var value = this.GetTag(key);
            if (value == null)
            {
                return default;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
