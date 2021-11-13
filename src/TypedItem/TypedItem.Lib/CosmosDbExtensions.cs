using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace TypedItem.Lib
{
    public static class CosmosDbExtensions
    {
        public static ItemRequestOptions WithSession(this ItemRequestOptions options, string sessionToken)
        {
            options.SessionToken = sessionToken;

            return options;
        }

        public static ItemRequestOptions WithETag<T>(this ItemRequestOptions options, T document) where T : ItemBase
        {
            options.IfMatchEtag = document.ETag;

            return options;
        }

        public static string GetSessionToken<T>(this ItemResponse<T> itemResponse)
        {
            return itemResponse.Headers.Session;
        }


        public static PartitionKey AsPartitionKey(this string partitionKey) => new PartitionKey(partitionKey);

        public static async Task<DataQueryResponse<TTo>> QueryTypedItemAsync<TFrom, TTo>(this Container container,
            Func<IQueryable<TFrom>, IQueryable<TTo>> queryFunc, QueryTypedItemsOptions? queryOptions = null, CancellationToken cancellationToken = default)
            where TFrom : TypedItemBase, new()
        {
            QueryRequestOptions options = new();
            
            queryOptions?.Fill(options);
            
            IQueryable<TFrom> fromQuery = container.GetItemLinqQueryable<TFrom>();
            
            if (queryOptions is { KeepDeleted: true })
            {
                fromQuery = fromQuery.Where(item => !item.Deleted);    
            }
            
            var iterator = queryFunc(fromQuery).ToFeedIterator();

            DataQueryResponse<TTo> result = null;
            
            var response = await iterator.ReadNextAsync(cancellationToken);

            result ??= new DataQueryResponse<TTo>
            {
                ContinuationToken = response.ContinuationToken,
                Results = new List<TTo>(response.Count)
            };

            foreach (var resultItem in response)
            {
                result.Results.Add(resultItem);
            }

            return result;
        }

        public static async Task<ItemResponse<T>> ReadTypedItemAsync<T>(this Container container,
            string id,
            PartitionKey partitionKey,
            TypedDocumentRequestOptions? options = null,
            CancellationToken cancellationToken = default) where T : TypedItemBase, new()
        {
            var readDeleted = options is { ReadDeleted: true };
            var result = await container.ReadItemAsync<T>(id, partitionKey, options, cancellationToken);
            if (result is null ||
                !readDeleted && result.Resource.Deleted ||
                !TypedItemHelper<T>.HasSameType(result.Resource))
            {
                throw new CosmosException("Not found", HttpStatusCode.NotFound, 0, string.Empty,
                    result?.RequestCharge ?? 0);
            }

            return result;
        }


        public static async Task<T> SoftDeleteTypedItemAsync<T>(this Container container,
            string id,
            PartitionKey partitionKey,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = new()) where T : TypedItemBase, new()
        {
            var result = await container.ReadItemAsync<T>(id, partitionKey, requestOptions, cancellationToken);
            if (result is null)
            {
                throw new CosmosException("Missing document", HttpStatusCode.NotFound, 0, null, 0);
            }

            var item = result.Resource;

            return await container.SoftDeleteTypedItemAsync(item, requestOptions, cancellationToken);
        }

        public static async Task<ItemResponse<T>> SoftDeleteTypedItemAsync<T>(this Container container,
            T item,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = default) where T : TypedItemBase
        {
            if (item.PartitionKey is null)
            {
                throw new ArgumentNullException("Item's pk is null");
            }

            if (item.Deleted)
            {
                throw new ArgumentException("Already deleted");
            }
            
            item.Deleted = true;
            var upsertOptions = requestOptions is not null ? requestOptions.Clone() : new ItemRequestOptions();

            upsertOptions.IfMatchEtag = item.ETag;

            var ret = await container.UpsertItemAsync(item, item.PartitionKey.AsPartitionKey(), upsertOptions,
                cancellationToken);

            return ret;
        }
    }
}


