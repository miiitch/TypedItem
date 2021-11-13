using Microsoft.Azure.Cosmos;

namespace TypedItem.Lib
{
    public class TypedDocumentRequestOptions : ItemRequestOptions
    {
        public bool ReadDeleted { get; set; } = false;
        
    }
}