using Microsoft.Azure.Cosmos;

namespace TypedItem.Lib
{
    public class TypedDocumentRequestOptions : ItemRequestOptions
    {
        public bool RetrieveDeleted { get; set; } = false;
        
    }
}