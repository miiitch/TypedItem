using Microsoft.Azure.Cosmos;

namespace TypedItem.Lib
{
    public record QueryTypedItemsOptions
    {
        public int? MaxItemCount { get; set; }
        public int? MaxConcurrency { get; set; }

        public bool GetAllPages { get; set; }
        
        public bool KeepDeleted { get; set; }

        internal void Fill(QueryRequestOptions options)
        {
            options.MaxItemCount = MaxItemCount;
            options.MaxConcurrency = MaxConcurrency;
        }

        
    }
}
