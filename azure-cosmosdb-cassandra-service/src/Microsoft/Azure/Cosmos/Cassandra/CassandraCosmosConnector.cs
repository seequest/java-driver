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
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Compute.Host;
    using Compute.Runtime;
    using Compute.Runtime.Transport;
    using Connectors.Cassandra.Service;
    using CosmosDB;
    using CosmosDB.Diagnostics;
    using CosmosDB.StateManagement;
    using CosmosDB.Transport;
    using CosmosDB.Transport.Http;
    using Documents;

    [SuppressMessage("Microsoft.Design",
        "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "TODO")]
    internal sealed class CassandraCosmosConnector
    {
        private const string Ipv6UriTemplate = "{0}://[{1}]:{2}/";
        private const string Ipv4UriTemplate = "{0}://{1}:{2}/";
        private readonly CassandraConnectorService cassandraConnectorService;
        private readonly List<ITransportHandler> transportHandlers;

        [SuppressMessage("Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope", Justification = "TODO")]
        public CassandraCosmosConnector(ICosmosDBConfigProvider configurationProvider,
            ICosmosDBDataProvider dataProvider,
            IStateManagerFactory stateManagerFactory, ICosmosDBHostRuntimeContext runtimeContext)
            : this(configurationProvider,
                new Compute.Host.CassandraCosmosConnector(configurationProvider, dataProvider, stateManagerFactory, runtimeContext),
                runtimeContext)
        { }

        [SuppressMessage("Microsoft.Reliability",
            "CA2000:Dispose objects before losing scope", Justification = "TODO")]
        internal CassandraCosmosConnector(ICosmosDBConfigProvider configurationProvider,
            IServiceProvider hostServiceProvider,
            ICosmosDBHostRuntimeContext runtimeContext)
        {
            RntbdExtensions.InitializeRntbdSettings(configurationProvider, originalSettings: null,
                currentSettings: out var currentSettings);

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
                // ReSharper disable once HeapView.BoxingAllocation
                ep.Address, ep.Port);

            var probeEP = new IPEndPoint(address, configurationProvider.HttpEndPointProbePort());
            var probePrefix = string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}/", probeEP.Address,
                // ReSharper disable once HeapView.BoxingAllocation
                probeEP.Port);

            CosmosDBTrace.TraceInformation("Started listening on address: " + prefix);
            CosmosDBTrace.TraceInformation("Started listening on probe address: " + probePrefix);

            var listenerUri = new Uri(prefix, UriKind.Absolute);
            var listenerUriProbe = new Uri(probePrefix, UriKind.Absolute);

            Uri[] httpListenerUris = {listenerUri, listenerUriProbe};
            bool isVNetFilterFeatureEnabled = configurationProvider.IsVnetFilterFeatureEnabled();
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

            string sslCertificateThumbprint = configurationProvider.RuntimeEndpointSslCertThumbprint();

            int? tcpKeepAliveTimeInMilliseconds = configurationProvider.GetTcpKeepAliveTimeInMilliseconds();
            int? tcpKeepAliveIntervalInMilliseconds = configurationProvider.GetKeepAliveIntervalInMilliseconds();

            this.transportHandlers = new List<ITransportHandler>(2)
            {
                new CosmosDBHttpRuntime(
                    configurationProvider.IsEmulated(),
                    httpListenerUris,
                    configurationProvider.ShouldForceShutdown()),
                new CosmosDBTcpRuntime(
                    enableTcp: configurationProvider.EnableTcp(),
                    isEmulated: configurationProvider.IsEmulated(),
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
            List<Task> openTasks = this.transportHandlers
                .Select(handler => handler.OpenAsync(this.HostServiceProvider, CancellationToken.None)).ToList();

            if (this.cassandraConnectorService != null)
            {
                openTasks.Add(this.cassandraConnectorService.OpenAsync(CancellationToken.None));
            }

            return Task.WhenAll(openTasks);
        }

        public Task CloseAsync()
        {
            List<Task> closeTasks = this.transportHandlers.Select(handler => handler.CloseAsync(CancellationToken.None))
                .ToList();

            if (this.cassandraConnectorService != null)
            {
                closeTasks.Add(this.cassandraConnectorService.CloseAsync());
            }

            return Task.WhenAll(closeTasks);
        }

        private static Uri GetIPv6ListenerUri(bool isEmulated, string protocol, int port)
        {
            if (!NetUtil.GetIPv6ServiceTunnelAddress(isEmulated, out var ipv6ServiceTunneledAddress))
            {
                return null;
            }

            var prefix = string.Format(CultureInfo.InvariantCulture, Ipv6UriTemplate,
                protocol,
                // ReSharper disable once HeapView.BoxingAllocation
                ipv6ServiceTunneledAddress.ToString(), port);

            return new Uri(prefix, UriKind.Absolute);
        }
    }
}