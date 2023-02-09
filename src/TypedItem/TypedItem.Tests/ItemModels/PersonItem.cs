using System;
using Newtonsoft.Json;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    [ItemType("person")]
    public sealed class PersonItem: ContainerItem
    {

        [JsonProperty("firstName")]
        public string FirstName { get; set; }
        
        [JsonProperty("lastName")]
        public string LastName { get; set; }
        
        [JsonProperty("birthdate")]
        public DateTime BirthDate { get; set; }
        
        [JsonProperty("index")]
        public int Index { get; set; }
    }
}
