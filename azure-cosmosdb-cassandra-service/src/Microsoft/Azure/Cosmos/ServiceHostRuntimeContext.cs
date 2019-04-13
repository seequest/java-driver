//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System.Net;
    using CosmosDB;

    /// <summary>
    ///     Runtime context for test host.
    /// </summary>
    internal sealed class ServiceHostRuntimeContext : ICosmosDBHostRuntimeContext
    {
        public string IPAddressOrFQDN => IPAddress.Loopback.ToString();

        public string NodeName => "CosmosDBGateway_IN_0";

        public string NodeType => "CosmosDBGateway";
    }
}