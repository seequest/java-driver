//-------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using Microsoft.Azure.CosmosDB.ServiceCommon;

    internal sealed class ServiceContainer : IServiceProvider
    {
        private readonly IServiceProvider hostProvider;
        private readonly ICosmosDBService cosmosDBService;

        public ServiceContainer(IServiceProvider hostProvider, ICosmosDBService service)
        {
            this.cosmosDBService = service;
            this.hostProvider = hostProvider;
        }

        public object GetService(Type serviceType)
        {
            return serviceType == typeof(ICosmosDBService)
                ? this.cosmosDBService
                : this.hostProvider.GetService(serviceType);
        }
    }
}