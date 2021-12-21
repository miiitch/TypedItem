using Microsoft.Azure.Cosmos;

namespace TypedItem.Lib
{
    public record QueryTypedItemsOptions
    {
        public int? MaxItemCount { get; set; }
        public int? MaxConcurrency { get; set; }

        public bool ReadAllPages { get; set; }
        
        public string? SessionToken { get; set; }
        
        public PartitionKey? PartitionKey { get; set; }

        public ConsistencyLevel? ConsistencyLevel { get; set; }
        
        public bool IncludeDeletedItems { get; set; }
        
        public string? ContinuationToken { get; set; }

        internal void Fill(QueryRequestOptions options)
        {
            options.MaxItemCount = MaxItemCount;
            options.MaxConcurrency = MaxConcurrency;
            options.SessionToken = SessionToken;
            options.ConsistencyLevel = ConsistencyLevel;
            options.PartitionKey = PartitionKey;
        }

    }
}
