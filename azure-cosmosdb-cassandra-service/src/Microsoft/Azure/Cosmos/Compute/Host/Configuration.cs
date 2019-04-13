//-------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using System.Diagnostics.Contracts;
    using CosmosDB;
    using CosmosDB.Utilities;

    internal static class HostConfiguration
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
        ///     Reads <see cref="HostConfigurationKeys.ComputeNodeCapacityInCpuClockCyclesPerSecond" /> parameter.
        /// </summary>
        /// <param name="provider">Cosmos DB configuration provider.</param>
        /// <returns>Compute node capacity in cpu clock cycles per second.</returns>
        public static long? ComputeNodeCapacityInCpuClockCyclesPerSecond(this ICosmosDBConfigProvider provider)
        {
            var computeNodeCapacityInCpuClockCyclesPerSecondParam = GetValue(
                HostConfigurationKeys.ComputeNodeCapacityInCpuClockCyclesPerSecond,
                provider);

            return long.TryParse(computeNodeCapacityInCpuClockCyclesPerSecondParam, out var result)
                ? (long?) result
                : null;
        }

        /// <summary>
        ///     Reads <see cref="HostConfigurationKeys.ComputeNodeCapacityInMemoryBytes" /> parameter.
        /// </summary>
        /// <param name="provider">Cosmos DB configuration provider.</param>
        /// <returns>Compute node capacity in memory bytes.</returns>
        public static long? ComputeNodeCapacityInMemoryBytes(this ICosmosDBConfigProvider provider)
        {
            var computeNodeCapacityInMemoryBytesParam = GetValue(
                HostConfigurationKeys.ComputeNodeCapacityInMemoryBytes,
                provider);

            return long.TryParse(computeNodeCapacityInMemoryBytesParam, out var result) ? (long?) result : null;
        }

        public static int GetNamingConfigRefreshIntervalInSeconds(this ICosmosDBConfigProvider provider)
        {
            var namingConfigRefreshIntervalInSeconds = 300;
            var namingConfigRefreshIntervalString = GetValue(
                HostConfigurationKeys.NamingConfigRefreshIntervalInSeconds,
                provider);
            if (string.IsNullOrEmpty(namingConfigRefreshIntervalString))
            {
                return namingConfigRefreshIntervalInSeconds;
            }

            namingConfigRefreshIntervalInSeconds = int.Parse(namingConfigRefreshIntervalString);
            return namingConfigRefreshIntervalInSeconds;
        }

        public static bool IsHttpEndPointEnabled(this ICosmosDBConfigProvider provider)
        {
            var isHttpEndPointEnabledString = GetValue(
                HostConfigurationKeys.IsHttpEndPointEnabled,
                provider);

            if (!string.IsNullOrWhiteSpace(isHttpEndPointEnabledString))
            {
                return bool.Parse(isHttpEndPointEnabledString);
            }

            return false;
        }

        public static string RegionName(this ICosmosDBConfigProvider provider)
        {
            var regionNameString = GetValue(
                HostConfigurationKeys.RegionName,
                provider);

            return regionNameString;
        }

        public static bool IsEmulated(this ICosmosDBConfigProvider provider)
        {
            var isEmulated = false;

            var emulatorConfig = GetValue(
                HostConfigurationKeys.IsEmulatedKey,
                provider);

            BoolTryParseNoReturn(emulatorConfig, out isEmulated);
            return isEmulated;
        }

        public static bool IsLocalEmulator(this ICosmosDBConfigProvider provider)
        {
            var isLocalEmulator = false;

            var emulatorConfig = GetValue(
                HostConfigurationKeys.IsLocalEmulatorKey,
                provider);

            BoolTryParseNoReturn(emulatorConfig, out isLocalEmulator);
            return isLocalEmulator;
        }

        public static string SqlDNSSuffix(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.SqlDNSSuffixKey,
                provider);
        }

        public static string TablesDNSSuffix(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.TablesDNSSuffix,
                provider);
        }

        public static bool IsTableEmulator(this ICosmosDBConfigProvider provider)
        {
            var isTableEmulator = false;

            var emulatorConfig = GetValue(
                HostConfigurationKeys.TableEmulatorMode,
                provider);

            BoolTryParseNoReturn(emulatorConfig, out isTableEmulator);
            return isTableEmulator;
        }

        public static string GraphDNSSuffix(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.GraphDNSSuffix,
                provider);
        }

        public static string QueryDNSSuffix(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.QueryDNSSuffix,
                provider);
        }

        public static int HttpEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var httpEndPointPortString = GetValue(
                HostConfigurationKeys.HttpEndPointPort,
                provider);

            return int.Parse(httpEndPointPortString);
        }

        public static int HttpEndPointProbePort(this ICosmosDBConfigProvider provider)
        {
            var httpEndPointProbePortString = GetValue(
                HostConfigurationKeys.HttpEndPointProbePort,
                provider);

            return int.Parse(httpEndPointProbePortString);
        }

        public static bool IsCassandraTcpEndpointEnabled(this ICosmosDBConfigProvider provider)
        {
            var isCassandraTcpEndpointEnabledString = GetValue(
                HostConfigurationKeys.IsCassandraTcpEndpointEnabled,
                provider);

            if (!string.IsNullOrWhiteSpace(isCassandraTcpEndpointEnabledString))
            {
                return bool.Parse(isCassandraTcpEndpointEnabledString);
            }

            return false;
        }

        public static bool IsGremlinEndpointEnabled(this ICosmosDBConfigProvider provider)
        {
            var isGremlinEndpointEnabledString = GetValue(
                HostConfigurationKeys.IsGremlinEndpointEnabled,
                provider);

            if (!string.IsNullOrWhiteSpace(isGremlinEndpointEnabledString))
            {
                return bool.Parse(isGremlinEndpointEnabledString);
            }

            return false;
        }

        public static bool IsTableEndpointEnabled(this ICosmosDBConfigProvider provider)
        {
            var isTableEndpointEnabledString = GetValue(
                HostConfigurationKeys.IsTableEndpointEnabled,
                provider);

            if (!string.IsNullOrWhiteSpace(isTableEndpointEnabledString))
            {
                return bool.Parse(isTableEndpointEnabledString);
            }

            return false;
        }

        public static bool IsMongoTcpEndpointEnabled(this ICosmosDBConfigProvider provider)
        {
            var isMongoTcpEndpointEnabled = GetValue(
                HostConfigurationKeys.IsMongoTcpEndpointEnabled,
                provider);

            if (!string.IsNullOrWhiteSpace(isMongoTcpEndpointEnabled))
            {
                return bool.Parse(isMongoTcpEndpointEnabled);
            }

            return false;
        }

        public static int MongoPrimaryEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var mongoPrimaryEndpointPort = GetValue(
                HostConfigurationKeys.MongoPrimaryEndpointPort,
                provider);

            return int.Parse(mongoPrimaryEndpointPort);
        }

        public static int CassandraEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var cassandraEndPointPortString = GetValue(
                HostConfigurationKeys.CassandraEndPointPort,
                provider);

            return int.Parse(cassandraEndPointPortString);
        }

        public static int GremlinEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var gremlinEndpointPort = GetValue(
                HostConfigurationKeys.GremlinEndPointPort,
                provider);

            return int.Parse(gremlinEndpointPort);
        }

        public static int TableEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var tableEndpointPort = GetValue(
                HostConfigurationKeys.TableEndPointPort,
                provider);

            return int.Parse(tableEndpointPort);
        }

        /// <summary>
        ///     Returns a value indicating whether to bind to localhost or public IP address.
        /// </summary>
        /// <param name="provider">Configuration provider to retrieve configuration settings about end-point.</param>
        public static bool ComputeLocalHostOnlyString(this ICosmosDBConfigProvider provider)
        {
            var isComputeLocalhostOnlyString =
                GetValue(HostConfigurationKeys.IsComputeLocalhostOnlySettingName, provider);

            if (!string.IsNullOrEmpty(isComputeLocalhostOnlyString) && bool.Parse(isComputeLocalhostOnlyString))
            {
                // We are binding to localhost
                return true;
            }

            return false;
        }

        public static int MongoSecondaryEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var mongoSecondaryEndpointPort = GetValue(
                HostConfigurationKeys.MongoSecondaryEndpointPort,
                provider);

            return int.Parse(mongoSecondaryEndpointPort);
        }

        public static int MongoRouterEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var mongoRouterEndpointPort = GetValue(
                HostConfigurationKeys.MongoRouterEndpointPort,
                provider);

            return int.Parse(mongoRouterEndpointPort);
        }

        public static string EtcdGrpcEndpoint(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.EtcdGrpcEndpointKey,
                provider);
        }

        public static string EchoDNSSuffix(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.EchoDNSSuffix,
                provider);
        }

        public static bool ShouldForceShutdown(this ICosmosDBConfigProvider provider)
        {
            var forceShutdown = false;

            var emulatorConfig = GetValue(
                HostConfigurationKeys.ForceShutdown,
                provider);

            BoolTryParseNoReturn(emulatorConfig, out forceShutdown);
            return forceShutdown;
        }

        public static bool EnableTcp(this ICosmosDBConfigProvider provider)
        {
            var tcpListenAddress = GetValue(
                HostConfigurationKeys.EnableTcpFlag,
                provider);
            if (string.IsNullOrEmpty(tcpListenAddress))
            {
                return false;
            }

            bool enableTcp;
            BoolTryParseNoReturn(tcpListenAddress, out enableTcp);
            return enableTcp;
        }

        public static string RuntimeEndpointSslCertThumbprint(this ICosmosDBConfigProvider provider)
        {
            var runtimeEndpointSslCertThumbprint = GetValue(
                HostConfigurationKeys.RuntimeEndpointSslCertThumbprintKey,
                provider);

            return runtimeEndpointSslCertThumbprint;
        }

        public static string PrimaryComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var decryptedKey = GetValue(
                HostConfigurationKeys.PrimaryComputeGatewayKeyDecrypted,
                provider);

            if (decryptedKey == null)
            {
                var value = DecryptKey(GetValue(
                    HostConfigurationKeys.PrimaryComputeGatewayKeyEncrypted,
                    provider));
                provider.AddKeyValue(HostConfigurationKeys.PrimaryComputeGatewayKeyDecrypted, value);

                return value;
            }

            return decryptedKey;
        }

        public static string SecondaryComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var decryptedKey = GetValue(
                HostConfigurationKeys.SecondaryComputeGatewayKeyDecrypted,
                provider);

            if (decryptedKey == null)
            {
                var value = DecryptKey(GetValue(
                    HostConfigurationKeys.SecondaryComputeGatewayKeyEncrypted,
                    provider));
                provider.AddKeyValue(HostConfigurationKeys.SecondaryComputeGatewayKeyDecrypted, value);

                return value;
            }

            return decryptedKey;
        }

        public static bool UseSecondaryComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var useSecondaryComputeGatewayKey = false;
            var config = GetValue(
                HostConfigurationKeys.UseSecondaryComputeGatewayKey,
                provider);
            BoolTryParseNoReturn(config, out useSecondaryComputeGatewayKey);
            return useSecondaryComputeGatewayKey;
        }

        public static bool ShouldSignRequestsWithComputeGatewayKey(this ICosmosDBConfigProvider provider)
        {
            var shouldSignRequestsWithComputeGatewayKey = false;
            var config = GetValue(
                HostConfigurationKeys.ShouldSignRequestsWithComputeGatewayKey,
                provider);
            BoolTryParseNoReturn(config, out shouldSignRequestsWithComputeGatewayKey);
            return shouldSignRequestsWithComputeGatewayKey;
        }

        public static int? GetTcpKeepAliveTimeInMilliseconds(this ICosmosDBConfigProvider provider)
        {
            var tcpKeepAliveTimeMilliseconds = GetValue(
                HostConfigurationKeys.TcpKeepAliveTimeInMillisecondsKey,
                provider);
            if (string.IsNullOrEmpty(tcpKeepAliveTimeMilliseconds))
            {
                return null;
            }

            int keepAliveMilliseconds;
            if (int.TryParse(tcpKeepAliveTimeMilliseconds, out keepAliveMilliseconds))
            {
                return keepAliveMilliseconds;
            }

            return null;
        }

        public static int? GetKeepAliveIntervalInMilliseconds(this ICosmosDBConfigProvider provider)
        {
            var tcpKeepAliveIntervalMilliseconds = GetValue(
                HostConfigurationKeys.TcpKeepAliveIntervalInMillisecondsKey,
                provider);
            if (string.IsNullOrEmpty(tcpKeepAliveIntervalMilliseconds))
            {
                return null;
            }

            int keepAliveMilliseconds;
            if (int.TryParse(tcpKeepAliveIntervalMilliseconds, out keepAliveMilliseconds))
            {
                return keepAliveMilliseconds;
            }

            return null;
        }

        public static string GetProtocol(this ICosmosDBConfigProvider provider)
        {
            var protocol = GetValue(
                HostConfigurationKeys.TransportProtocol,
                provider);

            if (string.IsNullOrEmpty(protocol))
            {
                return "https";
            }

            Contract.Assert(protocol == "http" || protocol == "https");
            return protocol;
        }

        public static string GetWebSocketProtocol(this ICosmosDBConfigProvider provider)
        {
            var protocol = GetValue(
                HostConfigurationKeys.WebSocketTransportProtocol,
                provider);

            if (string.IsNullOrEmpty(protocol))
            {
                return "wss";
            }

            Contract.Assert(protocol == "ws" || protocol == "wss");
            return protocol;
        }

        public static bool IsVnetFilterFeatureEnabled(this ICosmosDBConfigProvider provider)
        {
            var enableVnetFilter = false;
            var config = GetValue(
                HostConfigurationKeys.IsVNETFilterFeatureEnabled,
                provider);
            BoolTryParseNoReturn(config, out enableVnetFilter);
            return enableVnetFilter;
        }

        public static int GetChargeDispatchIntervalInMilliseconds(this ICosmosDBConfigProvider provider)
        {
            var chargeDispatchIntervalInMilliseconds = 300;
            var chargeDispatchInterval = GetValue(
                HostConfigurationKeys.ChargeDispatchIntervalInMilliseconds,
                provider);
            if (string.IsNullOrEmpty(chargeDispatchInterval))
            {
                return chargeDispatchIntervalInMilliseconds;
            }

            chargeDispatchIntervalInMilliseconds = int.Parse(chargeDispatchInterval);
            return chargeDispatchIntervalInMilliseconds;
        }

        public static int GetChargeValidityPeriodInMilliseconds(this ICosmosDBConfigProvider provider)
        {
            var chargeValidityPeriodInMilliSeconds = 1000;
            var chargeValidityPeriod = GetValue(
                HostConfigurationKeys.ChargeValidityPeriodInMilliseconds,
                provider);
            if (string.IsNullOrEmpty(chargeValidityPeriod))
            {
                return chargeValidityPeriodInMilliSeconds;
            }

            chargeValidityPeriodInMilliSeconds = int.Parse(chargeValidityPeriod);
            return chargeValidityPeriodInMilliSeconds;
        }

        public static int GetMaximumConcurrentRequestsForChargeTransfer(this ICosmosDBConfigProvider provider)
        {
            var maxConcurrentRequestsForChargeTransfer = 25;
            var maxConcurrentRequests = GetValue(
                HostConfigurationKeys.MaximumConcurrentRequestsForChargeTransfer,
                provider);
            if (string.IsNullOrEmpty(maxConcurrentRequests))
            {
                return maxConcurrentRequestsForChargeTransfer;
            }

            maxConcurrentRequestsForChargeTransfer = int.Parse(maxConcurrentRequests);
            return maxConcurrentRequestsForChargeTransfer;
        }

        /// <summary>
        ///     Windows Azure DNS zone for Cosmos DB Document API.
        ///     e.g., documents.azure.com.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static string DocumentDnsZone(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.DocumentDnsZone,
                provider);
        }

        /// <summary>
        ///     Windows Azure DNS zone for Cosmos DB Cassandra API.
        ///     e.g., cassandra.cosmosdb.azure.com.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static string CassandraDnsZone(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.CassandraDnsZone,
                provider);
        }

        /// <summary>
        ///     Windows Azure DNS zone for Cosmos DB Mongo API.
        ///     e.g., mongo.cosmosdb.azure.com.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static string MongoDnsZone(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.MongoDnsZone,
                provider);
        }

        /// <summary>
        ///     Windows Azure DNS zone for Cosmos DB Gremlin API.
        ///     e.g., gremlin.cosmosdb.azure.com.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static string GremlinDnsZone(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.GremlinDnsZone,
                provider);
        }

        /// <summary>
        ///     Windows Azure DNS zone for Cosmos DB Tables API.
        ///     e.g., table.cosmosdb.azure.com.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static string TableDnsZone(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.TableDnsZone,
                provider);
        }

        /// <summary>
        ///     Windows Azure DNS zone for Cosmos DB Etcd API.
        ///     e.g., etcd.cosmosdb.azure.com.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static string EtcdDnsZone(this ICosmosDBConfigProvider provider)
        {
            return GetValue(
                HostConfigurationKeys.EtcdDnsZone,
                provider);
        }

        public static bool IsEtcdEndpointEnabled(this ICosmosDBConfigProvider provider)
        {
            var isEtcdEnabledString = GetValue(
                HostConfigurationKeys.IsEtcdEndpointEnabled,
                provider);

            if (!string.IsNullOrWhiteSpace(isEtcdEnabledString))
            {
                return bool.Parse(isEtcdEnabledString);
            }

            return false;
        }

        public static int EtcdEndPointPort(this ICosmosDBConfigProvider provider)
        {
            var etcdEndPointPortString = GetValue(
                HostConfigurationKeys.EtcdGrpcEndpointPort,
                provider);

            return int.Parse(etcdEndPointPortString);
        }

        /// <summary>
        ///     Cache refresh interval for Geo api endpoints cache.
        /// </summary>
        /// <param name="provider"> Host Configuration provider.</param>
        public static TimeSpan RegionalEndpointsRefreshInterval(this ICosmosDBConfigProvider provider)
        {
            var value = GetValue(
                HostConfigurationKeys.RegionalEndpointRefreshIntervalInSec,
                provider);

            long timeInterval;
            TimeSpan intervalTimeSpan;

            if (value == null || !long.TryParse(value, out timeInterval))
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
                HostConfigurationKeys.RegionalEndpointInvalidIntervalInSec,
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
            return GetValue(
                HostConfigurationKeys.FederationReservedDnsName,
                provider);
        }

        public static bool IsCassandraConnectorServiceEnabled(this ICosmosDBConfigProvider provider)
        {
            var settingValueString = GetValue(
                HostConfigurationKeys.IsCassandraConnectorServiceEnabled,
                provider);

            return !string.IsNullOrWhiteSpace(settingValueString) && bool.Parse(settingValueString);
        }

        public static bool IsSharedStoreClientFactoryEnabled(this ICosmosDBConfigProvider provider)
        {
            var config = GetValue(
                HostConfigurationKeys.IsSharedStoreClientFactoryEnabled,
                provider);
            BoolTryParseNoReturn(config, out var enableSharedStoreClientFactory);
            return enableSharedStoreClientFactory;
        }

        public static int SharedStoreClientFactoryRequestTimeoutInSeconds(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryRequestTimeoutInSecondsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryRequestTimeoutInSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryRequestTimeoutInSecondsString)
                ? int.Parse(sharedStoreClientFactoryRequestTimeoutInSecondsString)
                : SharedStoreClientFactoryDefaultRequestTimeout;
        }

        public static int SharedStoreClientFactoryMaxConcurrentConnectionOpenRequests(
            this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryMaxConcurrentConnectionOpenRequestsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryMaxConcurrentConnectionOpenRequests,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryMaxConcurrentConnectionOpenRequestsString)
                ? int.Parse(sharedStoreClientFactoryMaxConcurrentConnectionOpenRequestsString)
                : Environment.ProcessorCount * SharedStoreClientFactoryMaxConcurrentConnectionOpenRequestsPerProcessor;
        }

        public static int SharedStoreClientFactoryOpenConnectionTimeoutInSeconds(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryOpenConnectionTimeoutInSecondsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryOpenConnectionTimeoutInSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryOpenConnectionTimeoutInSecondsString)
                ? int.Parse(sharedStoreClientFactoryOpenConnectionTimeoutInSecondsString)
                : SharedStoreClientFactoryDefaultOpenConnectionTimeoutInSeconds;
        }

        public static int SharedStoreClientFactoryIdleConnectionTimeoutInSeconds(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryIdleConnectionTimeoutInSecondsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryIdleConnectionTimeoutInSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryIdleConnectionTimeoutInSecondsString)
                ? int.Parse(sharedStoreClientFactoryIdleConnectionTimeoutInSecondsString)
                : SharedStoreClientFactoryDefaultIdleConnectionTimeoutInSeconds;
        }

        public static int SharedStoreClientFactoryTimerPoolGranularityInSeconds(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryTimerPoolGranularityInSecondsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryTimerPoolGranularityInSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryTimerPoolGranularityInSecondsString)
                ? int.Parse(sharedStoreClientFactoryTimerPoolGranularityInSecondsString)
                : SharedStoreClientFactoryDefaultTimerPoolGranularityInSeconds;
        }

        public static int SharedStoreClientFactoryMaxRntbdChannels(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryMaxRntbdChannelsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryMaxRntbdChannels,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryMaxRntbdChannelsString)
                ? int.Parse(sharedStoreClientFactoryMaxRntbdChannelsString)
                : SharedStoreClientFactoryDefaultMaxRntbdChannels;
        }

        public static int SharedStoreClientFactoryRntbdPartitionCount(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryRntbdPartitionCountString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryRntbdPartitionCount,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryRntbdPartitionCountString)
                ? int.Parse(sharedStoreClientFactoryRntbdPartitionCountString)
                : SharedStoreClientFactoryDefaultRntbdPartitionCount;
        }

        public static int SharedStoreClientFactoryMaxRequestsPerRntbdChannel(this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryMaxRequestsPerRntbdChannelString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryMaxRequestsPerRntbdChannel,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryMaxRequestsPerRntbdChannelString)
                ? int.Parse(sharedStoreClientFactoryMaxRequestsPerRntbdChannelString)
                : SharedStoreClientFactoryDefaultMaxRequestsPerRntbdChannel;
        }

        public static int SharedStoreClientFactoryRntbdReceiveHangDetectionTimeSeconds(
            this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryRntbdReceiveHangDetectionTimeSecondsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryRntbdReceiveHangDetectionTimeSeconds,
                provider);

            return !string.IsNullOrEmpty(sharedStoreClientFactoryRntbdReceiveHangDetectionTimeSecondsString)
                ? int.Parse(sharedStoreClientFactoryRntbdReceiveHangDetectionTimeSecondsString)
                : SharedStoreClientFactoryDefaultRntbdReceiveHangDetectionTimeSeconds;
        }

        public static int SharedStoreClientFactoryRntbdSendHangDetectionTimeSeconds(
            this ICosmosDBConfigProvider provider)
        {
            var sharedStoreClientFactoryRntbdSendHangDetectionTimeSecondsString = GetValue(
                HostConfigurationKeys.SharedStoreClientFactoryRntbdSendHangDetectionTimeSeconds,
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