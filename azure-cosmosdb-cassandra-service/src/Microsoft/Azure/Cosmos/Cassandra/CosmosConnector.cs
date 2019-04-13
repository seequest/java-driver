//------------------------------------------------------------
//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Compute.Host;
    using Microsoft.Azure.Cosmos.Compute.Runtime;
    using Microsoft.Azure.Cosmos.Compute.Runtime.Transport;
    using Microsoft.Azure.Cosmos.Connectors.Cassandra.Service;
    using Microsoft.Azure.CosmosDB;
    using Microsoft.Azure.CosmosDB.Diagnostics;
    using Microsoft.Azure.CosmosDB.StateManagement;
    using Microsoft.Azure.CosmosDB.Transport;
    using Microsoft.Azure.CosmosDB.Transport.Http;
    using Microsoft.Azure.Documents;

    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    internal sealed class CosmosConnector
    {
        private const string Ipv6UriTemplate = "{0}://[{1}]:{2}/";
        private const string Ipv4UriTemplate = "{0}://{1}:{2}/";

        private readonly CassandraConnectorService cassandraConnectorService;
        private readonly List<ITransportHandler> transportHandlers;

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public CosmosConnector(ICosmosDBConfigProvider configurationProvider,
            ICosmosDBDataProvider dataProvider,
            IStateManagerFactory stateManagerFactory, ICosmosDBHostRuntimeContext runtimeContext)
            : this(configurationProvider,
                new CosmosConnectorServiceProvider(configurationProvider, dataProvider, stateManagerFactory,
                    runtimeContext),
                runtimeContext)
        { }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        internal CosmosConnector(ICosmosDBConfigProvider configurationProvider,
            IServiceProvider hostServiceProvider,
            ICosmosDBHostRuntimeContext runtimeContext)
        {
            RntbdExtensions.InitializeRntbdSettings(configurationProvider, null, out var currentSettings);
            this.HostServiceProvider = hostServiceProvider;
            IPAddress address;

            if (configurationProvider.ComputeLocalHostOnlyString())
            {
                address = IPAddress.Loopback;
            }
            else
            {
                address = configurationProvider.IsEmulated()
                    ? IPAddress.Any
                    : IPAddress.Parse(runtimeContext.IPAddressOrFQDN);
            }

            var ep = new IPEndPoint(address, configurationProvider.HttpEndPointPort());
            var protocol = configurationProvider.GetProtocol();
            var prefix = string.Format(CultureInfo.InvariantCulture, Ipv4UriTemplate, protocol,
                ep.Address, ep.Port);

            var probeEP = new IPEndPoint(address, configurationProvider.HttpEndPointProbePort());
            var probePrefix = string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}/", probeEP.Address,
                probeEP.Port);

            CosmosDBTrace.TraceInformation("Started listening on address: " + prefix);
            CosmosDBTrace.TraceInformation("Started listening on probe address: " + probePrefix);

            var listenerUri = new Uri(prefix, UriKind.Absolute);
            var listenerUriProbe = new Uri(probePrefix, UriKind.Absolute);

            Uri[] httpListenerUris = {listenerUri, listenerUriProbe};
            var isVNetFilterFeatureEnabled = configurationProvider.IsVnetFilterFeatureEnabled();
            if (isVNetFilterFeatureEnabled)
            {
                var listenerUriIPv6 =
                    GetIPv6ListenerUri(configurationProvider.IsEmulated(), protocol, ep.Port);
                if (listenerUriIPv6 != null)
                {
                    CosmosDBTrace.TraceInformation("Started listening on address: " + listenerUriIPv6);
                    httpListenerUris = new[] {listenerUri, listenerUriProbe, listenerUriIPv6};
                }
                else
                {
                    CosmosDBTrace.TraceCritical(
                        "IPv6 Service tunneled address not found when VNet filter feature is enabled.");
                }
            }

            if (configurationProvider.IsLocalEmulator())
            {
                httpListenerUris =
                    EmulatorHelper.GetEmulatedListeners(configurationProvider, address, isVNetFilterFeatureEnabled);
            }

            var sslCertificateThumbprint = configurationProvider.RuntimeEndpointSslCertThumbprint();

            var tcpKeepAliveTimeInMilliseconds = configurationProvider.GetTcpKeepAliveTimeInMilliseconds();
            var tcpKeepAliveIntervalInMilliseconds = configurationProvider.GetKeepAliveIntervalInMilliseconds();

            this.transportHandlers = new List<ITransportHandler>(2)
            {
                new CosmosDBHttpRuntime(
                    configurationProvider.IsEmulated(),
                    httpListenerUris,
                    configurationProvider.ShouldForceShutdown()),
                new CosmosDBTcpRuntime(
                    configurationProvider.EnableTcp(),
                    configurationProvider.IsEmulated(),
                    shouldEnableIPv6RequestHandler: isVNetFilterFeatureEnabled,
                    shouldForceShutdown: configurationProvider.ShouldForceShutdown(),
                    sslCertificateThumbprint: sslCertificateThumbprint,
                    tcpKeepAliveTimeInMilliseconds: tcpKeepAliveTimeInMilliseconds,
                    tcpKeepAliveIntervalInMilliseconds: tcpKeepAliveIntervalInMilliseconds)
            };

            this.cassandraConnectorService = new CassandraConnectorService(this.HostServiceProvider);
        }

        internal IServiceProvider HostServiceProvider { get; }

        public Task OpenAsync()
        {
            var openTasks = this.transportHandlers
                .Select(handler => handler.OpenAsync(this.HostServiceProvider, CancellationToken.None)).ToList();

            if (this.cassandraConnectorService != null)
            {
                openTasks.Add(this.cassandraConnectorService.OpenAsync(CancellationToken.None));
            }

            return Task.WhenAll(openTasks);
        }

        public Task CloseAsync()
        {
            var closeTasks = this.transportHandlers.Select(handler => handler.CloseAsync(CancellationToken.None))
                .ToList();

            if (this.cassandraConnectorService != null)
            {
                closeTasks.Add(this.cassandraConnectorService.CloseAsync());
            }

            return Task.WhenAll(closeTasks);
        }

        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        private static Uri GetIPv6ListenerUri(bool isEmulated, string protocol, int port)
        {
            if (!NetUtil.GetIPv6ServiceTunnelAddress(isEmulated, out var ipv6ServiceTunneledAddress))
            {
                return null;
            }

            var prefix = string.Format(CultureInfo.InvariantCulture, Ipv6UriTemplate,
                protocol,
                ipv6ServiceTunneledAddress.ToString(), port);

            return new Uri(prefix, UriKind.Absolute);
        }
    }
}