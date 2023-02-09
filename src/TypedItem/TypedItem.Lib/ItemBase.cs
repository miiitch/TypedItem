using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace TypedItem.Lib
{
    public abstract class ItemBase
    {
        [JsonProperty("id")] 
        public string? Id { get; set; }
        
        
        [JsonProperty("_etag")]
        public string? ETag { get; set; }

        public abstract PartitionKey GetPartitionKey();
        
        protected static PartitionKey CreatePartitionKey(string partitionItem) => new PartitionKey(partitionItem);

    }
}