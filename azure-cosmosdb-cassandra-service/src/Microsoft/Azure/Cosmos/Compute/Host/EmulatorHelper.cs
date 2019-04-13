//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using System.Globalization;
    using System.Net;
    using CosmosDB;
    using CosmosDB.Diagnostics;
    using Documents;

    internal static class EmulatorHelper
    {
        private const string Ipv6UriTemplate = "{0}://[{1}]:{2}/";
        private const string Ipv4UriTemplate = "{0}://{1}:{2}/";

        internal static Uri[] GetEmulatedListeners(ICosmosDBConfigProvider configProvider, IPAddress address,
            bool isVNetFilterFeatureEnabled)
        {
            var httpListenerUris = Array.Empty<Uri>();
            return httpListenerUris;
        }

        private static Uri[] GetEmulatedHttpListeners(ICosmosDBConfigProvider configProvider, IPAddress address,
            bool isVNetFilterFeatureEnabled, string endpointName, string protocol, int endpointPort)
        {
            var uriPrefix = string.Format(CultureInfo.InvariantCulture, Ipv4UriTemplate, protocol, address,
                // ReSharper disable once HeapView.BoxingAllocation
                endpointPort);
            var listenerUri = new Uri(uriPrefix, UriKind.Absolute);

            CosmosDBTrace.TraceInformation($"Started listening for {endpointName} on address: {uriPrefix}");

            Uri[] httpListenerUris = {listenerUri};

            if (!isVNetFilterFeatureEnabled)
            {
                return httpListenerUris;
            }

            if (!NetUtil.GetIPv6ServiceTunnelAddress(configProvider.IsEmulated(), out var ipv6ServiceTunneledAddress))
            {
                return httpListenerUris;
            }

            var prefix = string.Format(CultureInfo.InvariantCulture, Ipv6UriTemplate, protocol,
                // ReSharper disable once HeapView.BoxingAllocation
                ipv6ServiceTunneledAddress.ToString(), endpointPort);
            
            var listenerUriIPv6 = new Uri(prefix, UriKind.Absolute);
            
            CosmosDBTrace.TraceInformation(
                $"Started listening for {endpointName} on address: {listenerUriIPv6}");
            
            return new[] {listenerUri, listenerUriIPv6};
        }
    }
}