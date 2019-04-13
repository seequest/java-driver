//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Cassandra;
    using Microsoft.Azure.CosmosDB;
    using Microsoft.Azure.CosmosDB.Diagnostics;
    using Microsoft.Azure.CosmosDB.ServiceCommon;
    using Microsoft.Azure.CosmosDB.UriMatch;
    using Microsoft.Extensions.DependencyInjection;

    internal class CosmosDBServiceResolver : ICosmosDBServiceResolver
    {
        private readonly IServiceProvider hostProvider;

        private readonly bool isLocalEmulator;

        private readonly Dictionary<string, IServiceProvider> serviceMapBySchemeAndPort;

        private readonly Dictionary<string, Dictionary<ICosmosDBUriTemplate, IServiceProvider>>
            serviceMapBySchemeAndUri;

        private readonly CosmosDBUriTemplateFactory uriTemplateFactory = new CosmosDBUriTemplateFactory();

        public CosmosDBServiceResolver(IServiceProvider hostProvider)
        {
            this.hostProvider = hostProvider;
            this.serviceMapBySchemeAndUri =
                new Dictionary<string, Dictionary<ICosmosDBUriTemplate, IServiceProvider>>(StringComparer
                    .OrdinalIgnoreCase);
            this.serviceMapBySchemeAndPort = new Dictionary<string, IServiceProvider>(StringComparer.OrdinalIgnoreCase);

            var configProvider = hostProvider.GetService<ICosmosDBConfigProvider>();
            this.isLocalEmulator = configProvider?.IsLocalEmulator() ?? false;
        }

        public IServiceProvider ResolveService(IServiceProvider hostProvider, Uri requestUri)
        {
            IServiceProvider serviceProvider = null;
            if (this.isLocalEmulator)
            {
                if (this.serviceMapBySchemeAndPort.TryGetValue($"{requestUri.Scheme}:{requestUri.Port}",
                    out serviceProvider))
                {
                    CosmosDBTrace.TraceVerbose("Returning service {0} for uri {1}",
                        serviceProvider.GetService<ICosmosDBService>().ServiceName, requestUri.DnsSafeHost);
                    return serviceProvider;
                }
            }
            else
            {
                if (this.serviceMapBySchemeAndUri.TryGetValue(requestUri.Scheme, out var serviceMapByUriTemplate))
                {
                    var uriTemplate = this.uriTemplateFactory.CreateTemplateFromUri(requestUri);
                    if (uriTemplate != null && serviceMapByUriTemplate.TryGetValue(uriTemplate, out serviceProvider))
                    {
                        CosmosDBTrace.TraceVerbose("Returning service {0} for uri {1}",
                            serviceProvider.GetService<ICosmosDBService>().ServiceName, requestUri.DnsSafeHost);
                        return serviceProvider;
                    }
                }
            }

            CosmosDBTrace.TraceError("No service resolved for uri {0}", requestUri.AbsoluteUri);

            throw new ApplicationException(); //InternalServerErrorException(RMResources.InternalServerError);
        }

        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        public void RegisterService(ICosmosDBService service)
        {
            var serviceContainer = new ServiceContainer(
                this.hostProvider,
                service);

            foreach (var listenUri in service.TransportListenUris)
            {
                if (this.isLocalEmulator && listenUri.GetType() == typeof(CosmosDBSchemeAndPortUriTemplate))
                {
                    var schemeAndPortUriTemplate = (CosmosDBSchemeAndPortUriTemplate) listenUri;
                    var uri = new Uri($"{schemeAndPortUriTemplate.Scheme}://{schemeAndPortUriTemplate.Endpoint}");

                    if (!this.serviceMapBySchemeAndPort.ContainsKey($"{uri.Scheme}:{uri.Port}"))
                    {
                        this.serviceMapBySchemeAndPort[$"{uri.Scheme}:{uri.Port}"] = serviceContainer;
                    }
                    else
                    {
                        CosmosDBTrace.TraceError("Listening service for {0} already registered",
                            $"{uri.Scheme}:{uri.Port}");
                    }
                }

                Dictionary<ICosmosDBUriTemplate, IServiceProvider> serviceMapByUriTemplate;
                if (!this.serviceMapBySchemeAndUri.TryGetValue(listenUri.Scheme, out serviceMapByUriTemplate))
                {
                    serviceMapByUriTemplate = new Dictionary<ICosmosDBUriTemplate, IServiceProvider>();
                    this.serviceMapBySchemeAndUri[listenUri.Scheme] = serviceMapByUriTemplate;
                }

                serviceMapByUriTemplate.Add(listenUri, serviceContainer);
            }
        }

        public IEnumerable<ICosmosDBUriTemplate> GetRegisteredUriTemplates()
        {
            return this.serviceMapBySchemeAndUri.Values.SelectMany(uriTemplateKvp => uriTemplateKvp.Keys);
        }
    }
}