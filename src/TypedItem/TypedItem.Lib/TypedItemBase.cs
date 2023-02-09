using System.ComponentModel;
using Newtonsoft.Json;

namespace TypedItem.Lib
{
    public abstract class TypedItemBase : ItemBase
    {
        [JsonProperty("_deleted")] public bool Deleted { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("_type")] 
        public string? ItemType { get; set; }

        
    }
}
