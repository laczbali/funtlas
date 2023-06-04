using Newtonsoft.Json;

namespace OpenStreetMap.API.Models
{
    public class Way
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public List<long> Nodes { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public WayRank? GetRank()
        {
            var rankStr = this.GetTag("highway")?.ToUpper();
            if (rankStr is null)
            {
                return null;
            }

            if (Enum.TryParse(rankStr, out WayRank rank))
            {
                return rank;
            }
            return null;
        }

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
            string? value = string.Empty;
            try
            {
                value = this.GetTag(key);
                if (value == null)
                {
                    return default;
                }

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value;
                }

                return JsonConvert.DeserializeObject<T>(value);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to convert tag [{key}] with value of [{value}] to type [{typeof(T).Name}]", e);
            }
        }
    }
}
