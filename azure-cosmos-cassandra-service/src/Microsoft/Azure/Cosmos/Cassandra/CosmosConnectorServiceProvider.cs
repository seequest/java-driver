//-------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using Microsoft.Azure.Cosmos.Cassandra.Service;
    using Microsoft.Azure.Cosmos.Compute.Host;
    using Microsoft.Azure.Cosmos.Compute.Runtime;
    using Microsoft.Azure.CosmosDB;
    using Microsoft.Azure.CosmosDB.Common;
    using Microsoft.Azure.CosmosDB.Diagnostics;
    using Microsoft.Azure.CosmosDB.ServiceCommon;
    using Microsoft.Azure.CosmosDB.StateManagement;
    using Microsoft.Azure.CosmosDB.UriMatch;
    using Microsoft.Azure.CosmosDB.Utilities;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.IO;
    
    #if POSIX
    using ComputeEventSource = Microsoft.Azure.Cosmos.Posix.Diagnostics.ComputeEventSource;

    #elif Windows_NT
    using ComputeEventSource = Microsoft.Azure.Cosmos.Diagnostics.ComputeEventSource;

    #else
    #error Expected one of these compilation symbol sets to be defined: Darwin, POSIX; Linux, POSIX; or Windows_NT

    #endif

    internal sealed class CosmosConnectorServiceProvider : IServiceProvider, IDisposable
    {
        private const string IpRangeListByRegionSettingsFileName = "IpRangeListByRegion.json";
        private const int MaxBufferPoolSize = 100 * 1024 * 1024;
        private const int MaxTransportBufferSize = 2 * 1024 * 1024;

        private readonly ICosmosDBAuthorizer authorizer;
        private readonly IBackendAuthenticator backendAuthenticator;
        private readonly IBufferManager bufferManager;
        private readonly FirewallAuthorizer firewallAuthorizer;
        private readonly ICosmosDBConfigProvider hostConfigurationProvider;
        private readonly ICosmosDBHostRuntimeContext hostRuntimeContext;
        private readonly bool isEmulated;
        private readonly INetworkIPRangePolicy networkIpRangePolicy;
        private readonly CosmosDBRequestPipeline pipeline;
        private readonly NodeResourceGovernor resourceGovernor;
        private readonly CosmosDbServiceUriTemplateEnumerator servicePortResolver;
        private readonly CosmosDBServiceResolver serviceResolver;
        private readonly IStateManagerFactory stateManagerFactory;
        private readonly IStoreClientFactory storeClientFactory;
        private readonly RecyclableMemoryStreamManager streamManager;
        private readonly TimerPool timerPool;

        private string computeGatewayKey;
        private bool isDisposed;

        public CosmosConnectorServiceProvider(ICosmosDBConfigProvider hostConfigProvider,
            ICosmosDBDataProvider hostDataProvider, IStateManagerFactory stateManagerFactory,
            ICosmosDBHostRuntimeContext hostRuntimeContext)
            : this(hostConfigProvider, hostDataProvider, new TenantConfigBasedAuthenticator(), stateManagerFactory,
                hostRuntimeContext)
        { }

        private CosmosConnectorServiceProvider(ICosmosDBConfigProvider hostConfigProvider,
            ICosmosDBDataProvider hostDataProvider, IBackendAuthenticator authenticator,
            IStateManagerFactory stateManagerFactory, ICosmosDBHostRuntimeContext hostRuntimeContext)
        {
            this.hostConfigurationProvider = hostConfigProvider;
            this.isEmulated = this.hostConfigurationProvider.IsEmulated();
            this.serviceResolver = new CosmosDBServiceResolver(this);
            this.servicePortResolver = new CosmosDbServiceUriTemplateEnumerator(this.serviceResolver);
            this.timerPool = new TimerPool(1);
            this.resourceGovernor = new NodeResourceGovernor(this);
            this.isDisposed = false;
            this.authorizer = new CosmosDBAuthorizer();
            this.firewallAuthorizer = new FirewallAuthorizer(hostConfigProvider.IsEmulated());
            this.pipeline = new CosmosDBRequestPipeline(this, hostConfigProvider.IsEmulated(),
                hostConfigProvider.EnablePerformanceCounters());
            this.bufferManager = BufferManager.Create(MaxBufferPoolSize, MaxTransportBufferSize, false);
            this.streamManager = new RecyclableMemoryStreamManager();
            this.backendAuthenticator = authenticator;
            this.networkIpRangePolicy = new NetworkIPRangePolicy(hostConfigProvider.IsVnetFilterFeatureEnabled());
            this.computeGatewayKey = this.GetComputeGatewayKeyFromConfig();
            this.stateManagerFactory = stateManagerFactory;
            this.hostRuntimeContext = hostRuntimeContext;

            // Shared store client factory is controlled by a configuration setting

            if (this.hostConfigurationProvider.IsSharedStoreClientFactoryEnabled())
            {
                CosmosDBTrace.TraceInformation("Compute gateway process is using a shared store client factory");

                var userAgentContainer = new UserAgentContainer
                {
                    Suffix = nameof(CosmosConnectorServiceProvider)
                };

                // A single store client factory will be shared among all document clients created in process.
                // Store client factory creates a single transport client in constructor and shares it among all store clients it creates.
                // This allows better transport utilization among all document clients needing to connect to the same backend nodes from the same compute nodes.
                this.storeClientFactory = new StoreClientFactory(
                    Protocol.Tcp,
                    this.hostConfigurationProvider.SharedStoreClientFactoryRequestTimeoutInSeconds(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryMaxConcurrentConnectionOpenRequests(),
                    userAgentContainer,
                    null, // The compute gateway test code uses DocumentClientEventSource.Instance but it's not exposed on netstandard2.0
                    null,
                    this.hostConfigurationProvider.SharedStoreClientFactoryOpenConnectionTimeoutInSeconds(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryIdleConnectionTimeoutInSeconds(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryTimerPoolGranularityInSeconds(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryMaxRntbdChannels(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryRntbdPartitionCount(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryMaxRequestsPerRntbdChannel(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryRntbdReceiveHangDetectionTimeSeconds(),
                    this.hostConfigurationProvider.SharedStoreClientFactoryRntbdSendHangDetectionTimeSeconds(),
                    false); // CPU Monitor must be disabled for compute gateway because it spins up a thread to monitor CPU to enrich exceptions, which is unnecessary for compute gateway
            }

            if (!string.IsNullOrEmpty(hostConfigProvider.RegionName()))
            {
                // Subscribe for notifications about changes to data package that contains IPRangeFilter file
                hostDataProvider.OnDataChanged += this.OnDataChanged;
                var dataPath = hostDataProvider.DataPath;

                if (!string.IsNullOrEmpty(dataPath))
                {
                    // Perform initialization
                    this.OnDataChanged(this,
                        new CosmosDBDataProviderEventArgs(CosmosDBDataProviderEventType.Added, dataPath));
                }
            }

            this.Initialize();
            this.EnsureTimer();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(ICosmosDBAuthorizer))
            {
                return this.authorizer;
            }

            if (serviceType == typeof(ICosmosDBServiceResolver))
            {
                return this.serviceResolver;
            }

            if (serviceType == typeof(ICosmosDBRequestPipeline))
            {
                return this.pipeline;
            }

            if (serviceType == typeof(ICosmosDBConfigProvider))
            {
                return this.hostConfigurationProvider;
            }

            if (serviceType == typeof(IBufferManager))
            {
                return this.bufferManager;
            }

            if (serviceType == typeof(RecyclableMemoryStreamManager))
            {
                return this.streamManager;
            }

            if (serviceType == typeof(TimerPool))
            {
                return this.timerPool;
            }

            if (serviceType == typeof(ICosmosDBServiceUriTemplateEnumerator))
            {
                return this.servicePortResolver;
            }

            if (serviceType == typeof(IResourceGovernor))
            {
                return this.resourceGovernor;
            }

            if (serviceType == typeof(IBackendAuthenticator))
            {
                return this.backendAuthenticator;
            }

            if (serviceType == typeof(INetworkIPRangePolicy))
            {
                return this.networkIpRangePolicy;
            }

            if (serviceType == typeof(IStateManagerFactory))
            {
                return this.stateManagerFactory;
            }

            if (serviceType == typeof(IStoreClientFactory))
            {
                return this.storeClientFactory;
            }

            if (serviceType == typeof(IComputeEventSource))
            {
                return ComputeEventSource.Log;
            }

            if (serviceType == typeof(FirewallAuthorizer))
            {
                return this.firewallAuthorizer;
            }

            if (serviceType == typeof(ICosmosDBHostRuntimeContext))
            {
                return this.hostRuntimeContext;
            }

            CosmosDBTrace.TraceCritical("Unexpected service type {0} requested", serviceType);
            throw new NotSupportedException();
        }

        private void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing)
            {
                this.timerPool.Dispose();
                this.storeClientFactory?.Dispose();
            }

            this.isDisposed = true;
        }

        private void Initialize()
        {
            // This code MUST match ServiceStartup code
            // If ComputeLocalHost is true we bind to localhost
            // Otherwise, if we are running in emulated mode--this means AllowNetworkAccess is true--we bind to 0.0.0.0

            IPAddress address;

            if (this.hostConfigurationProvider.ComputeLocalHostOnlyString())
            {
                address = IPAddress.Loopback;
            }
            else
            {
                address = this.isEmulated ? IPAddress.Any : IPAddress.Parse(this.hostRuntimeContext.IPAddressOrFQDN);
            }

            this.RegisterServiceForCassandra(address);
        }


        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        private void RegisterServiceForCassandra(IPAddress address)
        {
            this.serviceResolver.RegisterService(
                new CassandraService(new[]
                {
                    new CosmosDBSchemeAndPortUriTemplate(
                        CosmosDBUriConstants.TcpScheme,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}:{1}",
                            address,
                            this.hostConfigurationProvider.CassandraEndPointPort()))
                }));
        }

        private void EnsureTimer()
        {
            try
            {
                this.timerPool.GetPooledTimer(
                        this.hostConfigurationProvider.GetNamingConfigRefreshIntervalInSeconds()).StartTimerAsync()
                    .ContinueWith(
                        task =>
                        {
                            if (task.IsFaulted)
                            {
                                CosmosDBTrace.TraceError("Timer failed with exception {0}", task.Exception);
                            }
                            else
                            {
                                this.OnTimer();
                            }
                        }).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                CosmosDBTrace.TraceInformation("Timer pool is disposed, process is shutting down");
            }
        }

        private void OnTimer()
        {
            try
            {
                var computeGatewayKeyFromConfig = this.GetComputeGatewayKeyFromConfig();

                if (!string.Equals(this.computeGatewayKey, computeGatewayKeyFromConfig, StringComparison.Ordinal))
                {
                    this.computeGatewayKey = computeGatewayKeyFromConfig;
                }
            }
            finally
            {
                this.EnsureTimer();
            }
        }

        private string GetComputeGatewayKeyFromConfig()
        {
            return this.hostConfigurationProvider.UseSecondaryComputeGatewayKey()
                ? this.hostConfigurationProvider.SecondaryComputeGatewayKey()
                : this.hostConfigurationProvider.PrimaryComputeGatewayKey();
        }

        /// <summary>Notification handler for changes to data package on the file system</summary>
        /// <param name="sender">Sender of the notification.</param>
        /// <param name="args">Details of the change.</param>
        private void OnDataChanged(object sender, CosmosDBDataProviderEventArgs args)
        {
            if (args.EventType != CosmosDBDataProviderEventType.Added &&
                args.EventType != CosmosDBDataProviderEventType.Refreshed)
            {
                return;
            }

            // Check if we have data package
            var ipRangeListByRegionSettingsFile = Path.Combine(args.Path, IpRangeListByRegionSettingsFileName);
            this.networkIpRangePolicy.Initialize(ipRangeListByRegionSettingsFile,
                this.hostConfigurationProvider.RegionName(), this.hostConfigurationProvider.IsEmulated());
        }
    }
}