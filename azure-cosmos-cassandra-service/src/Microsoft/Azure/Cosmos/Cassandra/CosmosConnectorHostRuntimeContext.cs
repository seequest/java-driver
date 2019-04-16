//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System.Net;
    using Microsoft.Azure.CosmosDB;

    /// <summary>
    ///     Runtime context for test host.
    /// </summary>
    internal sealed class CosmosConnectorHostRuntimeContext : ICosmosDBHostRuntimeContext
    {
        public string IPAddressOrFQDN => IPAddress.Loopback.ToString();

        public string NodeName => "CosmosDBGateway_IN_0";

        public string NodeType => "CosmosDBGateway";
    }
}