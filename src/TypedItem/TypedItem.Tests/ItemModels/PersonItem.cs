using Newtonsoft.Json;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    public class PersonItem: TypedItemBase
    {
        public PersonItem() : base("person")
        {
        }
        
        [JsonProperty("firstName")]
        public string FirstName { get; set; }
        
        [JsonProperty("lastName")]
        public string LastName { get; set; }
    }
}
