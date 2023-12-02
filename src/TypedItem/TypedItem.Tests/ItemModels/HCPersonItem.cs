using System;
using Newtonsoft.Json;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    [ItemType("person")]
    public sealed class HCPersonItem: HierarchicalContainerItem
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
    
    [ItemType("event")]
    public class HCEventItem: HierarchicalContainerItem
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
    
    [ItemType("phonecall")]
    public sealed class HCPhonecallItem: HCEventItem
    {

        [JsonProperty("duration")]
        public int Duration { get; set; }
    }

    [ItemType("teamsmeeting")]
    public sealed class HCTeamsMeeting: HCEventItem
    {

        [JsonProperty("peoples")]
        public string[] Peoples { get; set; }
    }
}
