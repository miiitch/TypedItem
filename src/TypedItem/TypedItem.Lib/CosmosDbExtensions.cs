using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Linq;
using TypedItem.Lib;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Cosmos
{
    public static class CosmosDbExtensions
    {
        public static ItemRequestOptions WithETag<T>(this ItemRequestOptions options, T item) where T : ItemBase
        {
            options.IfMatchEtag = item.ETag;

            return options;
        }
        
        public static PartitionKey AsPartitionKey(this string partitionKey) => new PartitionKey(partitionKey);

        public static Task<ItemResponse<T>> UpsertTypedItemAsync<T>(this Container container,
            T item,
            PartitionKey? partitionKey = null,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = default) where T: TypedItemBase, new()
        {
            TypedItemHelper<T>.PrepareItem(item);
            return container.UpsertItemAsync(item, partitionKey, requestOptions, cancellationToken);
        }
        
        public static TransactionalBatch UpsertTypedItem<T>(this TransactionalBatch batch,
            T item,
            TransactionalBatchItemRequestOptions? requestOptions = null) where T: TypedItemBase, new()
        {
            TypedItemHelper<T>.PrepareItem(item);
            return batch.UpsertItem(item, requestOptions);
        }
        
        
        
        public static Task<ItemResponse<T>> ReplaceTypedItemAsync<T>(this Container container,
            T item,
            string id,
            PartitionKey? partitionKey = null,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = default) where T: TypedItemBase, new()
        {
            TypedItemHelper<T>.PrepareItem(item);
            return container.ReplaceItemAsync(item, id, partitionKey, requestOptions, cancellationToken);
        }
        
        public static TransactionalBatch ReplaceTypedItem<T>(this TransactionalBatch batch,
            T item,
            TransactionalBatchItemRequestOptions? requestOptions = null) where T: TypedItemBase, new()
        {
            TypedItemHelper<T>.PrepareItem(item);
            return batch.ReplaceItem(item.Id,item, requestOptions);
        }

     
       
        
        public static Task<ItemResponse<T>> CreateTypedItemAsync<T>(this Container container,
            T item,
            PartitionKey? partitionKey = null,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = default) where T: TypedItemBase, new()
        {
            TypedItemHelper<T>.PrepareItem(item);
            return container.CreateItemAsync(item, partitionKey, requestOptions, cancellationToken);
        }
        
        public static TransactionalBatch CreateTypedItem<T>(this TransactionalBatch batch,
            T item,
            TransactionalBatchItemRequestOptions? requestOptions = null) where T: TypedItemBase, new()
        {
            TypedItemHelper<T>.PrepareItem(item);
            return batch.CreateItem(item, requestOptions);
        }

        
        public static async Task<DataQueryResponse<TTo>> QueryTypedItemAsync<TFrom, TTo>(this Container container,
            Func<IQueryable<TFrom>, IQueryable<TTo>> queryFunc, QueryTypedItemsOptions? queryOptions = null, CancellationToken cancellationToken = default)
            where TFrom : TypedItemBase, new()
        {
            var sourceType = TypedItemHelper<TFrom>.ItemType;
            var sourceIsFinal = TypedItemHelper<TFrom>.IsFinal;
            QueryRequestOptions options = new();
            
            queryOptions?.Fill(options);
            
            IQueryable<TFrom> fromQuery = container.GetItemLinqQueryable<TFrom>(
                requestOptions: options, 
                continuationToken:queryOptions?.ContinuationToken);
            
            if (queryOptions is { IncludeDeletedItems: false })
            {
                fromQuery = fromQuery.Where(item => !item.Deleted);    
            }

            if (sourceIsFinal)
            {
                fromQuery = fromQuery.Where(_ => _.ItemType == sourceType);
            }
            else
            {
                var typeToSearch = $"{sourceType}.";
                fromQuery = fromQuery.Where(_ => _.ItemType!.StartsWith(typeToSearch));
            }
            
            
            var iterator = queryFunc(fromQuery).ToFeedIterator();

            DataQueryResponse<TTo>? result = null;
            
            await ReadFeedIteratorAsync(queryOptions?.ReadAllPages == false);

            if (queryOptions?.ReadAllPages == true)
            {
                while (iterator.HasMoreResults)
                {
                    await ReadFeedIteratorAsync(false);
                }
            }

            return result!;

            async Task ReadFeedIteratorAsync(bool setContinuationToken)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);

                result ??= new DataQueryResponse<TTo>(new List<TTo>(response.Count), setContinuationToken?response.ContinuationToken:null);

                result.RequestCharge += response.RequestCharge;
                foreach (var resultItem in response)
                {
                    result.Results.Add(resultItem); 
                }

                result.SessionToken = response.Headers.Session;
            }
        }

        public static async Task<ItemResponse<T>> ReadTypedItemAsync<T>(this Container container,
            string id,
            PartitionKey partitionKey,
            TypedItemRequestOptions? options = null,
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


        public static Task<ItemResponse<T>> SoftDeleteTypedItemAsync<T>(this Container container,
            string id,
            PartitionKey partitionKey,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = new()) where T : TypedItemBase, new()
        {
            var patchOperations = new[]
            {
                PatchOperation.Replace("/_deleted", true)
            };

            var patchOptions = new PatchItemRequestOptions();
            requestOptions?.Fill(patchOptions);
            patchOptions.FilterPredicate = "FROM item WHERE NOT item._deleted";
            return container.PatchItemAsync<T>(id, partitionKey, patchOperations, patchOptions, cancellationToken);
        }
        
        public static TransactionalBatch SoftDeleteTypedItem(this TransactionalBatch batch, string id,  TransactionalBatchPatchItemRequestOptions? requestOptions = null)
        {
            var patchOperations = new[]
            {
                PatchOperation.Replace("/_deleted", true)
            };

            requestOptions ??= new();
            requestOptions.FilterPredicate = "FROM item WHERE NOT item._deleted";
            return batch.PatchItem(id, patchOperations, requestOptions);
        }


        public static Task<ItemResponse<T>> SoftDeleteTypedItemAsync<T>(this Container container,
            T item,
            ItemRequestOptions? requestOptions = null,
            CancellationToken cancellationToken = default) where T : TypedItemBase, new()
        {
            if (item.Id is null)
            {
                // ReSharper disable once NotResolvedInText
                throw new ArgumentNullException("item.id");
            }
            
            if (item.PartitionKey is null)
            {
                // ReSharper disable once NotResolvedInText
                throw new ArgumentNullException("item.PartitionKey");
            }

            if (item.Deleted)
            {
                throw new ArgumentException("Already deleted");
            }

            requestOptions ??= new ItemRequestOptions();
            requestOptions.IfMatchEtag = item.ETag;

            return container.SoftDeleteTypedItemAsync<T>(item.Id, item.PartitionKey.AsPartitionKey(), requestOptions, cancellationToken);
        }

        private static void Fill(this ItemRequestOptions itemRequestOptions,
            PatchItemRequestOptions patchItemRequestOptions)
        {
            patchItemRequestOptions.Properties = itemRequestOptions.Properties;
            patchItemRequestOptions.ConsistencyLevel = itemRequestOptions.ConsistencyLevel;
            patchItemRequestOptions.IndexingDirective = itemRequestOptions.IndexingDirective;
            patchItemRequestOptions.PostTriggers = itemRequestOptions.PostTriggers;
            patchItemRequestOptions.PreTriggers = itemRequestOptions.PreTriggers;
            patchItemRequestOptions.SessionToken = itemRequestOptions.SessionToken;
            patchItemRequestOptions.IfMatchEtag = itemRequestOptions.IfMatchEtag;
            patchItemRequestOptions.IfNoneMatchEtag = itemRequestOptions.IfNoneMatchEtag;
            patchItemRequestOptions.EnableContentResponseOnWrite = itemRequestOptions.EnableContentResponseOnWrite;
        }
    }


    
    
    
}


