using Newtonsoft.Json;

namespace TypedItem.Lib
{
    public class TypedItemBase: ItemBase
    {

        [JsonProperty("_deleted")] public bool Deleted { get; set; }
        [JsonProperty("_type")] public string DocumentType { get; }


        public TypedItemBase(string type)
        {
            DocumentType = type;
        }
    }
}