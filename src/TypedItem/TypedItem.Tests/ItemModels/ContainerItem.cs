using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels;

public class ContainerItem : TypedItemBase
{
    [JsonProperty("part")]
    public string Part { get; set; }
        
    public override PartitionKey GetPartitionKey()
    {
        return CreatePartitionKey(Part);
    }
}