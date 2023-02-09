using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    [ItemType("address")]
    public sealed class AddressItem : ContainerItem
    {
  
        
        public string City { get; set; }
    }
}
