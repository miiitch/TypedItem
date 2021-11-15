using TypedItem.Lib;

namespace TypedItem.Tests.ItemModels
{
    public class AddressItem : TypedItemBase
    {
        public AddressItem():base("address")
        {
            
        }
        
        public string City { get; set; }
    }
}
