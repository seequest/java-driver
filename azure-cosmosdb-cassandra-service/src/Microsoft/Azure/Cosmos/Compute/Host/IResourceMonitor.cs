//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using Microsoft.Azure.CosmosDB;

    internal interface IResourceMonitor : IDisposable
    {
        LoadMetric CurrentLoad { get; }
    }
}
