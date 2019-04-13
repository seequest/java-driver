//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.CosmosDB.StateManagement
{
    using System;

    /// <summary>
    /// StateItem class reflects the item that is required to store it into state manager.
    /// </summary>
    /// <remarks>Actual Storage of the item in State Manager is not the same as State Item. For best performance, Total size of the object should be tried to kept as minimum as possible.</remarks>
    internal sealed class StateItem
    {
        public StateItem()
        {
        }

        public StateItem(uint? partitionKey, string key, byte[] value, TimeSpan? timeToLive)
        {
            this.PartitionKey = partitionKey;
            this.Key = key;
            this.Value = value;
            this.TimeToLive = timeToLive;
        }

        /// <summary>
        /// PartitionKey is the location where the item will be stored in State Manager. For Fabric Store, it refers to partition key of the stateful service.
        /// For Cosmos Store, the partition key can be used to store the item in desired partition.
        /// </summary>
        /// <remarks>This is nullable. In case partition key is not provided, that indicates to state manager that localisation is desired and appropriate partition key
        /// need to be calculated by State Manager. In case its provided, then that is the location considered to store the item. </remarks>
        public uint? PartitionKey { get; set; }

        /// <summary>
        /// Key should be small and unique. </param>
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///  Value is the value to be stored in classic Key-Value pair semantics. Should be tried to kept as minimum as possible.
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        /// Time To Live for item. Caller must specify it. It applies on the last modified time of the item.
        /// </summary>
        /// <remarks>
        /// Server might not return it back as response and therefore its nullable.
        /// </remarks>
        public TimeSpan? TimeToLive { get; set; }
    }
}
