using System;
using Newtonsoft.Json;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    [ItemType("event")]
    public class EventItem: TypedItemBase
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
    
    [ItemType("phonecall")]
    public sealed class PhonecallItem: EventItem
    {

        [JsonProperty("duration")]
        public int Duration { get; set; }
    }

    [ItemType("teamsmeeting")]
    public sealed class TeamsMeeting: EventItem
    {

        [JsonProperty("peoples")]
        public string[] Peoples { get; set; }
    }

    [ItemType("typeA")]
    public sealed class TypeAEventItem: EventItem
    {
        
    }
    
   
    public class TypeBEventItem: EventItem
    {
        
    }

    [ItemType("typeB1")]
    public sealed class TypeB1EventItem : TypeBEventItem
    {
        
    }

    public class CustomAndInvalidItem : TypedItemBase
    {
        
    }
}
