//-------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using Microsoft.Azure.CosmosDB.ServiceCommon;

    internal sealed class ServiceContainer : IServiceProvider
    {
        private readonly ICosmosDBService cosmosDbService;
        private readonly IServiceProvider hostProvider;

        public ServiceContainer(IServiceProvider hostProvider, ICosmosDBService service)
        {
            this.cosmosDbService = service;
            this.hostProvider = hostProvider;
        }

        public object GetService(Type serviceType)
        {
            return serviceType == typeof(ICosmosDBService)
                ? this.cosmosDbService
                : this.hostProvider.GetService(serviceType);
        }
    }
}