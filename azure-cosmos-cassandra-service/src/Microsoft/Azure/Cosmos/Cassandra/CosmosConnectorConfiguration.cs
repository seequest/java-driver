//-------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System;
    using System.Diagnostics.Contracts;
    using Microsoft.Azure.CosmosDB;
    using Microsoft.Azure.CosmosDB.Utilities;

    internal static class CosmosConnectorConfiguration
    {
        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryMaxConcurrentConnectionOpenRequestsPerProcessor = 25;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultRequestTimeout = 60;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultOpenConnectionTimeoutInSeconds = 5;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultIdleConnectionTimeoutInSeconds = -1;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultTimerPoolGranularityInSeconds = 1;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultMaxRntbdChannels = ushort.MaxValue;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultRntbdPartitionCount = 1;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultMaxRequestsPerRntbdChannel = 30;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultRntbdReceiveHangDetectionTimeSeconds = 65;

        /// <summary>
        ///     Default value from DocumentClient.
        /// </summary>
        private const int SharedStoreClientFactoryDefaultRntbdSendHangDetectionTimeSeconds = 10;

        /// <summary>
        ///     Reads <see cref="CosmosConnectorHostConfigurationKeys.ComputeNodeCapacityInCpuClockCyclesPerSecond" /> parameter.
        /// </summary>
        /// <param name="provider">Cosmos DB configuration provider.</param>
        /// <returns>Compute node capacity in cpu clock cycles per second.</returns>
        public static long? ComputeNodeCapacityInCpuClockCyclesPerSecond(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.ComputeNodeCapacityInCpuClockCyclesPerSecond,
                provider);
            return long.TryParse(value, out var result) ? (long?) result : null;
        }

        /// <summary>
        ///     Reads <see cref="CosmosConnectorHostConfigurationKeys.ComputeNodeCapacityInMemoryBytes" /> parameter.
        /// </summary>
        /// <param name="provider">Cosmos DB configuration provider.</param>
        /// <returns>Compute node capacity in memory bytes.</returns>
        public static long? ComputeNodeCapacityInMemoryBytes(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.ComputeNodeCapacityInMemoryBytes, provider);
            return long.TryParse(value, out var result) ? (long?) result : null;
        }

        public static int GetNamingConfigRefreshIntervalInSeconds(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.NamingConfigRefreshIntervalInSeconds, provider);
            const int namingConfigRefreshIntervalInSeconds = 300;
            return string.IsNullOrEmpty(value) ? namingConfigRefreshIntervalInSeconds : int.Parse(value);
        }

        public static bool EnablePerformanceCounters(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.EnablePerformanceCounters, provider);
            BoolTryParseNoReturn(value, out var enablePerformanceCounters);
            return enablePerformanceCounters;
        }
        
        public static bool IsHttpEndPointEnabled(this ICosmosDBConfigProvider provider)
        {
            var isHttpEndPointEnabledString =
                GetValue(CosmosConnectorHostConfigurationKeys.IsHttpEndPointEnabled, provider);
            return !string.IsNullOrWhiteSpace(isHttpEndPointEnabledString) && bool.Parse(isHttpEndPointEnabledString);
        }

        public static string RegionName(this ICosmosDBConfigProvider provider)
        {
            return GetValue(CosmosConnectorHostConfigurationKeys.RegionName, provider);
        }

        public static bool IsEmulated(this ICosmosDBConfigProvider provider)
        {
            var emulatorConfig = GetValue(CosmosConnectorHostConfigurationKeys.IsEmulatedKey, provider);
            BoolTryParseNoReturn(emulatorConfig, out var isEmulated);
            return isEmulated;
        }

        public static bool IsLocalEmulator(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.IsLocalEmulatorKey, provider);
            BoolTryParseNoReturn(value, out var isLocalEmulator);
            return isLocalEmulator;
        }

        public static int HttpEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.HttpEndPointPort, provider);
            return int.Parse(value);
        }

        public static int HttpEndPointProbePort(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.HttpEndPointProbePort, provider);
            return int.Parse(value);
        }

        public static int CassandraEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var cassandraEndPointPortString =
                GetValue(CosmosConnectorHostConfigurationKeys.CassandraEndPointPort, provider);
            return int.Parse(cassandraEndPointPortString);
        }

        /// <summary>
        ///     Returns a value indicating whether to bind to localhost or public IP address.
        /// </summary>
        /// <param name="provider">Configuration provider to retrieve configuration settings about end-point.</param>
        public static bool ComputeLocalHostOnlyString(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.IsComputeLocalhostOnlySettingName, provider);
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }

        public static bool ShouldForceShutdown(this ICosmosDBConfigProvider provider)
        {
            var emulatorConfig = GetValue(CosmosConnectorHostConfigurationKeys.ForceShutdown, provider);
            BoolTryParseNoReturn(emulatorConfig, out var forceShutdown);
            return forceShutdown;
        }

        public static bool EnableTcp(this ICosmosDBConfigProvider provider)
        {
            var tcpListenAddress = GetValue(CosmosConnectorHostConfigurationKeys.EnableTcpFlag, provider);

            if (string.IsNullOrEmpty(tcpListenAddress))
            {
                return false;
            }

            BoolTryParseNoReturn(tcpListenAddress, out var enableTcp);
            return enableTcp;
        }

        public static string RuntimeEndpointSslCertThumbprint(this ICosmosDBConfigProvider provider)
        {
            return GetValue(CosmosConnectorHostConfigurationKeys.RuntimeEndpointSslCertThumbprintKey, provider);
        }

        public static string PrimaryComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var decryptedKey = GetValue(CosmosConnectorHostConfigurationKeys.PrimaryComputeGatewayKeyDecrypted,
                provider);

            if (decryptedKey != null)
            {
                return decryptedKey;
            }

            var value = DecryptKey(GetValue(CosmosConnectorHostConfigurationKeys.PrimaryComputeGatewayKeyEncrypted,
                provider));
            provider.AddKeyValue(CosmosConnectorHostConfigurationKeys.PrimaryComputeGatewayKeyDecrypted, value);

            return value;
        }

        public static string SecondaryComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var decryptedKey = GetValue(CosmosConnectorHostConfigurationKeys.SecondaryComputeGatewayKeyDecrypted,
                provider);

            if (decryptedKey != null)
            {
                return decryptedKey;
            }

            var value = DecryptKey(GetValue(CosmosConnectorHostConfigurationKeys.SecondaryComputeGatewayKeyEncrypted,
                provider));
            provider.AddKeyValue(CosmosConnectorHostConfigurationKeys.SecondaryComputeGatewayKeyDecrypted, value);

            return value;
        }

        public static bool UseSecondaryComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.UseSecondaryComputeGatewayKey, provider);
            BoolTryParseNoReturn(value, out var useSecondaryComputeGatewayKey);
            return useSecondaryComputeGatewayKey;
        }

        public static bool ShouldSignRequestsWithComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.ShouldSignRequestsWithComputeGatewayKey,
                provider);
            BoolTryParseNoReturn(value, out var shouldSignRequestsWithComputeGatewayKey);
            return shouldSignRequestsWithComputeGatewayKey;
        }

        public static int? GetTcpKeepAliveTimeInMilliseconds(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.TcpKeepAliveTimeInMillisecondsKey, provider);

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (int.TryParse(value, out var tcpKeepAliveTimeInMilliseconds))
            {
                return tcpKeepAliveTimeInMilliseconds;
            }

            return null;
        }

        public static int? GetKeepAliveIntervalInMilliseconds(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.TcpKeepAliveIntervalInMillisecondsKey, provider);

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (int.TryParse(value, out var keepAliveIntervalInMilliseconds))
            {
                return keepAliveIntervalInMilliseconds;
            }

            return null;
        }

        public static string GetProtocol(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.TransportProtocol, provider);

            if (string.IsNullOrEmpty(value))
            {
                return "https";
            }

            Contract.Assert(value == "http" || value == "https");
            return value;
        }

        public static string GetWebSocketProtocol(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.WebSocketTransportProtocol, provider);

            if (string.IsNullOrEmpty(value))
            {
                return "wss";
            }

            Contract.Assert(value == "ws" || value == "wss");
            return value;
        }

        public static bool IsVnetFilterFeatureEnabled(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.IsVNETFilterFeatureEnabled, provider);
            BoolTryParseNoReturn(value, out var enableVnetFilter);
            return enableVnetFilter;
        }

        /// <summary>
        ///     Windows Azure DNS zone for Cosmos DB Cassandra API.
        ///     e.g., cassandra.cosmosdb.azure.com.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static string CassandraDnsZone(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                CosmosConnectorHostConfigurationKeys.CassandraDnsZone,
                provider);
        }

        /// <summary>
        ///     Cache refresh interval for Geo api endpoints cache.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static TimeSpan RegionalEndpointsRefreshInterval(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(
                CosmosConnectorHostConfigurationKeys.RegionalEndpointRefreshIntervalInSec,
                provider);

            TimeSpan intervalTimeSpan;

            if (value == null || !long.TryParse(value, out var timeInterval))
            {
                intervalTimeSpan = ComputeGatewayConstants.RegionalEndpointRefreshInterval;
            }
            else
            {
                intervalTimeSpan = TimeSpan.FromSeconds(timeInterval);
            }

            return intervalTimeSpan;
        }

        /// <summary>
        ///     Cache invlidation interval for Geo api endpoints cache.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static TimeSpan RegionalEndpointsInvalidationInterval(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(
                CosmosConnectorHostConfigurationKeys.RegionalEndpointInvalidIntervalInSec,
                provider);

            long timeInterval;
            TimeSpan intervalTimeSpan;

            if (value == null || !long.TryParse(value, out timeInterval))
            {
                intervalTimeSpan = ComputeGatewayConstants.RegionalEndpointInvalidInterval;
            }
            else
            {
                intervalTimeSpan = TimeSpan.FromSeconds(timeInterval);
            }

            return intervalTimeSpan;
        }

        public static string FederationReservedDnsName(this ICosmosDBConfigProvider provider)
        {
            return GetValue(CosmosConnectorHostConfigurationKeys.FederationReservedDnsName, provider);
        }

        public static bool IsSharedStoreClientFactoryEnabled(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.IsSharedStoreClientFactoryEnabled, provider);
            BoolTryParseNoReturn(value, out var enableSharedStoreClientFactory);
            return enableSharedStoreClientFactory;
        }

        public static int SharedStoreClientFactoryRequestTimeoutInSeconds(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryRequestTimeoutInSeconds,
                provider);
            return !string.IsNullOrEmpty(value) ? int.Parse(value) : SharedStoreClientFactoryDefaultRequestTimeout;
        }

        public static int SharedStoreClientFactoryMaxConcurrentConnectionOpenRequests(
            this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryMaxConcurrentConnectionOpenRequests,
                provider);

            return !string.IsNullOrEmpty(value)
                ? int.Parse(value)
                : Environment.ProcessorCount * SharedStoreClientFactoryMaxConcurrentConnectionOpenRequestsPerProcessor;
        }

        public static int SharedStoreClientFactoryOpenConnectionTimeoutInSeconds(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryOpenConnectionTimeoutInSecondsString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryOpenConnectionTimeoutInSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryOpenConnectionTimeoutInSecondsString)
                ? int.Parse(sharedStoreClientFactoryOpenConnectionTimeoutInSecondsString)
                : SharedStoreClientFactoryDefaultOpenConnectionTimeoutInSeconds;
        }

        public static int SharedStoreClientFactoryIdleConnectionTimeoutInSeconds(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryIdleConnectionTimeoutInSecondsString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryIdleConnectionTimeoutInSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryIdleConnectionTimeoutInSecondsString)
                ? int.Parse(sharedStoreClientFactoryIdleConnectionTimeoutInSecondsString)
                : SharedStoreClientFactoryDefaultIdleConnectionTimeoutInSeconds;
        }

        public static int SharedStoreClientFactoryTimerPoolGranularityInSeconds(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryTimerPoolGranularityInSecondsString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryTimerPoolGranularityInSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryTimerPoolGranularityInSecondsString)
                ? int.Parse(sharedStoreClientFactoryTimerPoolGranularityInSecondsString)
                : SharedStoreClientFactoryDefaultTimerPoolGranularityInSeconds;
        }

        public static int SharedStoreClientFactoryMaxRntbdChannels(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryMaxRntbdChannelsString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryMaxRntbdChannels,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryMaxRntbdChannelsString)
                ? int.Parse(sharedStoreClientFactoryMaxRntbdChannelsString)
                : SharedStoreClientFactoryDefaultMaxRntbdChannels;
        }

        public static int SharedStoreClientFactoryRntbdPartitionCount(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryRntbdPartitionCountString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryRntbdPartitionCount,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryRntbdPartitionCountString)
                ? int.Parse(sharedStoreClientFactoryRntbdPartitionCountString)
                : SharedStoreClientFactoryDefaultRntbdPartitionCount;
        }

        public static int SharedStoreClientFactoryMaxRequestsPerRntbdChannel(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryMaxRequestsPerRntbdChannelString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryMaxRequestsPerRntbdChannel,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryMaxRequestsPerRntbdChannelString)
                ? int.Parse(sharedStoreClientFactoryMaxRequestsPerRntbdChannelString)
                : SharedStoreClientFactoryDefaultMaxRequestsPerRntbdChannel;
        }

        public static int SharedStoreClientFactoryRntbdReceiveHangDetectionTimeSeconds(
            this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryRntbdReceiveHangDetectionTimeSecondsString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryRntbdReceiveHangDetectionTimeSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryRntbdReceiveHangDetectionTimeSecondsString)
                ? int.Parse(sharedStoreClientFactoryRntbdReceiveHangDetectionTimeSecondsString)
                : SharedStoreClientFactoryDefaultRntbdReceiveHangDetectionTimeSeconds;
        }

        public static int SharedStoreClientFactoryRntbdSendHangDetectionTimeSeconds(
            this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryRntbdSendHangDetectionTimeSecondsString = GetValue(
                CosmosConnectorHostConfigurationKeys.SharedStoreClientFactoryRntbdSendHangDetectionTimeSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryRntbdSendHangDetectionTimeSecondsString)
                ? int.Parse(sharedStoreClientFactoryRntbdSendHangDetectionTimeSecondsString)
                : SharedStoreClientFactoryDefaultRntbdSendHangDetectionTimeSeconds;
        }

        private static string GetValue(string key, ICosmosDBConfigProvider provider)
        {
            return provider.TryGetValue(key, out var value) ? value : null;
        }

        private static string DecryptKey(string encryptedKey)
        {
            return string.IsNullOrEmpty(encryptedKey) ? null : EncryptionUtility.DecryptString(encryptedKey);
        }

        #pragma warning disable CA1806 // Do not ignore method results

        private static void BoolTryParseNoReturn(string text, out bool value)
        {
            bool.TryParse(text, out value);
        }

        #pragma warning restore CA1806 // Do not ignore method results
    }
}