using System;
using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    [ItemType("event")]
    public class AppEvent: TypedItemBase
    {
        public DateTime Date { get; set; }
    }

    [ItemType("typeA")]
    public sealed class TypeAEvent: AppEvent
    {
        
    }
    
   
    public class TypeBEvent: AppEvent
    {
        
    }

    [ItemType("typeB1")]
    public sealed class TypeB1Event : TypeBEvent
    {
        
    }

    public class CustomDocument : TypedItemBase
    {
        
    }
}
