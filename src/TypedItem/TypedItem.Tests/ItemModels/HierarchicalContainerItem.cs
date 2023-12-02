using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels;

public class HierarchicalContainerItem : TypedItemBase
{
    [JsonProperty("part")]
    public string Part { get; set; }
    
    [JsonProperty("subPart")]
    public string SubPart { get; set; }
        
    public override PartitionKey GetPartitionKey()
    {
        return CreatePartitionKey(Part, SubPart);
    }
}