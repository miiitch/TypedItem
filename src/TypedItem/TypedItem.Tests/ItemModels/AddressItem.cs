using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    [ItemType("address")]
    public sealed class AddressItem : TypedItemBase
    {
  
        
        public string City { get; set; }
    }
}
