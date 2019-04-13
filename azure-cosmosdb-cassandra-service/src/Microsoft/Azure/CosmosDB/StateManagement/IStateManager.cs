//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.CosmosDB.StateManagement
{
    using System.Threading.Tasks;

    /// <summary>
    /// IStateManager Interface is facade b/w calling service (Interop) and the actual store.
    /// This interface can be implemented by underlying stores like Fabric based Reliable Collections, Cosmos DB System Collections etc.
    /// By default classes that implement this interface assumes that localisation/colocation is desired for better latency. In those cases, partition key in item should not be passed.
    /// </summary>
    internal interface IStateManager
    {
        /// <summary>
        /// Adds a item into the State Manager if the key does not already exist, or updates a item in the State Manager if the key already exists.
        /// </summary>
        /// <param name="item">1. item.key to kept as minimum as possible for better performance.
        /// 2.item.Value should be kept as minimum as possible for better performance.
        /// 3.item.TimeToLive with null value or item having TimeToLive greater than uppper Bound supported by the State Manager will throw BadRequest status code.
        /// 4.item.PartitionKey is the key where the data would be stored in Store. If localisation is desired, caller should set it null so that classes that implement will calculate appropriate partition key for localisation</param>
        /// <returns>1. StateManagerResponse.StatusCode are relevant mapping to HttpStatusCodes.
        /// 2. StateManagerResponse.Item might not be fully populated. StateManagerResponse.Item.PartitionKey would be returned to caller</returns>
        Task<StateManagerResponse> UpsertAsync(StateItem item);

        /// <summary>
        /// Tries to Get the item from State Manager.
        /// </summary>
        /// <param name="key">key with which the item was stored into State Manager</param>
        /// <param name="partitionKey">partitionKey with which the item was stored into State Manager</param>
        /// <returns>1. StateManagerResponse.StatusCode are relevant mapping to HttpStatusCodes. If key is not found, the status code would be NotFound.
        /// 2. StateManagerResponse.Item has the actual desired item. If item could not be found, this would be null.</returns>
        Task<StateManagerResponse> TryGetAsync(string key, uint partitionKey);

        /// <summary>
        /// Tries to Removes the item from State Manager.
        /// </summary>
        /// <param name="key">key with which the item was stored into State Manager</param>
        /// <param name="partitionKey">partitionKey with which the item was stored into State Manager</param>
        /// <returns>1. StateManagerResponse.StatusCode are relevant mapping to HttpStatusCodes. If key is not found, the status code would be NotFound.
        /// 2. StateManagerResponse.Item might not be populated. Caller to ensure null checks on response.</returns>
        Task<StateManagerResponse> TryRemoveAsync(string key, uint partitionKey);
    }
}
