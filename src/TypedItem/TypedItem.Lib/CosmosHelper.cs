using Microsoft.Azure.Cosmos;

namespace TypedItem.Lib
{
    public static class CosmosHelper {

        public static ItemRequestOptions Clone(this ItemRequestOptions options) =>
            new()
            {
                ConsistencyLevel = options.ConsistencyLevel,
                EnableContentResponseOnWrite = options.EnableContentResponseOnWrite,
                IfMatchEtag = options.IfMatchEtag,
                IfNoneMatchEtag = options.IfNoneMatchEtag,
                IndexingDirective = options.IndexingDirective,
                PostTriggers = options.PostTriggers,
                PreTriggers = options.PreTriggers,
                Properties = options.Properties,
                SessionToken = options.SessionToken,
            };
    }
}