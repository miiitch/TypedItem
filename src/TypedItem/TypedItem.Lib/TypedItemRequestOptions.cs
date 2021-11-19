using Microsoft.Azure.Cosmos;

namespace TypedItem.Lib
{
    public class TypedItemRequestOptions : ItemRequestOptions
    {
        public bool ReadDeleted { get; set; } = false;
        
    }
}