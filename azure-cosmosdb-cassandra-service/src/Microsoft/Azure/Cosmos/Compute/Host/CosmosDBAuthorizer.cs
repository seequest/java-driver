//-------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-------------------------------------------------------------------------------------------------------------------------------
namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using Microsoft.Azure.CosmosDB.ServiceCommon;

    internal sealed class CosmosDBAuthorizer : ICosmosDBAuthorizer
    {
        public void AuthorizeRequest(CosmosDBRequest request, IServiceProvider tenantProvider, out IServiceProvider requestProvider)
        {
            throw new NotImplementedException();
        }
    }
}