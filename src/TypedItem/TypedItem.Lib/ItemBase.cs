using Newtonsoft.Json;

namespace TypedItem.Lib
{
    public class ItemBase
    {
        [JsonProperty("id")] 
        public string? Id { get; set; }
        
        [JsonProperty("_pk")] 
        public string? PartitionKey { get; set; }
        
        [JsonProperty("_etag")]
        public string? ETag { get; set; }
    }
}