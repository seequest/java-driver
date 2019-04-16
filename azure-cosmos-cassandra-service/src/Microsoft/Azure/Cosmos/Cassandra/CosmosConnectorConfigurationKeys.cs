//-------------------------------------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-------------------------------------------------------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    internal static class CosmosConnectorHostConfigurationKeys
    {
        /// <summary>
        ///     Name of configuration property inside service settings that indicates whether compute gateway should bind to local
        ///     host interface only rather than expose port to extra-machine traffic.
        /// </summary>
        public const string IsComputeLocalhostOnlySettingName = "isComputeLocalhostOnly";

        /// <summary>
        ///     Configuration property name that indicates whether regular Http end-point is enabled on compute gateway.
        /// </summary>
        public const string IsHttpEndPointEnabled = "IsHttpEndPointEnabled";

        /// <summary>
        ///     Name of configuration property inside service settings that contains the port to which to bind HTTP end-point.
        /// </summary>
        public const string HttpEndPointPort = "httpEndPointPort";

        /// <summary>
        ///     Name of configuration property inside service settings that contains the port to which to bind HTTP probe
        ///     end-point.
        /// </summary>
        public const string HttpEndPointProbePort = "httpEndPointProbePort";

        /// <summary>
        ///     Configuration property name that indicates the cassandra end-point port setting.
        /// </summary>
        public const string CassandraEndPointPort = "cassandraEndPointPort";

        /// <summary>
        ///     Compute node capacity in cpu clock cycles per second.
        /// </summary>
        public const string ComputeNodeCapacityInCpuClockCyclesPerSecond =
            "computeNodeCapacityInCpuClockCyclesPerSecond";

        /// <summary>
        ///     Compute node capacity in memory bytes.
        /// </summary>
        public const string ComputeNodeCapacityInMemoryBytes = "computeNodeCapacityInMemoryBytes";

        public const string NamingConfigRefreshIntervalInSeconds = "namingConfigRefreshIntervalInSeconds";

        public const string ForceShutdown = "forceShutdown";
        public const string TransportProtocol = "transportProtocol";
        public const string WebSocketTransportProtocol = "webSocketTransportProtocol";
        public const string EnableTcpFlag = "enableTcp";
        public const string TcpKeepAliveTimeInMillisecondsKey = "tcpKeepAliveTimeInMilliseconds";
        public const string TcpKeepAliveIntervalInMillisecondsKey = "tcpKeepAliveIntervalInMilliseconds";
        public const string IsEmulatedKey = "isEmulated";
        public const string IsLocalEmulatorKey = "isLocalEmulator";

        public const string RuntimeEndpointSslCertThumbprintKey = "runtimeEndpointSslCertThumbprint";

        public const string RegionName = "regionName";
        public const string IsVNETFilterFeatureEnabled = "enableVnetFilterFeature";
        public const string PrimaryComputeGatewayKeyEncrypted = "PrimaryComputeGatewayKeyEncrypted";
        public const string SecondaryComputeGatewayKeyEncrypted = "SecondaryComputeGatewayKeyEncrypted";
        public const string UseSecondaryComputeGatewayKey = "UseSecondaryComputeGatewayKey";
        public const string PrimaryComputeGatewayKeyDecrypted = "PrimaryComputeGatewayKeyDecrypted";
        public const string SecondaryComputeGatewayKeyDecrypted = "SecondaryComputeGatewayKeyDecrypted";
        public const string ShouldSignRequestsWithComputeGatewayKey = "ShouldSignRequestsWithComputeGatewayKey";

        public const string CassandraDnsZone = "WaCassandraDnsZone";
        public const string RegionalEndpointRefreshIntervalInSec = "RegionalEndpointRefreshIntervalInSec";
        public const string RegionalEndpointInvalidIntervalInSec = "RegionalEndpointInvalidIntervalInSec";

        public const string FederationReservedDnsName = "reservedDnsName";

        /// <summary>
        ///     Name of configuration setting that enables a single transport client for all V2 SDK inside compute gateway process.
        /// </summary>
        public const string IsSharedStoreClientFactoryEnabled = "isSharedStoreClientFactoryEnabled";

        /// <summary>
        ///     Name of configuration setting that controls request timeout in seconds for shared transport client for V2 SDK.
        /// </summary>
        public const string SharedStoreClientFactoryRequestTimeoutInSeconds =
            "sharedStoreClientFactoryRequestTimeoutInSeconds";

        /// <summary>
        ///     Name of configuration setting that specifies the number of concurrent connection open requests per single transport
        ///     client.
        /// </summary>
        public const string SharedStoreClientFactoryMaxConcurrentConnectionOpenRequests =
            "sharedStoreClientFactoryMaxConcurrentConnectionOpenRequests";

        /// <summary>
        ///     Name of configuration setting that specifies the timeout for connection open in seconds for shared transport client
        ///     for V2 SDK inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryOpenConnectionTimeoutInSeconds =
            "sharedStoreClientFactoryOpenConnectionTimeoutInSeconds";

        /// <summary>
        ///     Name of configuration setting that specifies the timeout for idle connection termination in seconds for shared
        ///     transport client for V2 SDK inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryIdleConnectionTimeoutInSeconds =
            "sharedStoreClientFactoryIdleConnectionTimeoutInSeconds";

        /// <summary>
        ///     Name of configuration setting that specifies the frequency at which timer pool runs in seconds in a single
        ///     transport client for V2 SDK inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryTimerPoolGranularityInSeconds =
            "sharedStoreClientFactoryTimerPoolGranularityInSeconds";

        /// <summary>
        ///     Name of configuration setting that specifies the number of RNTBD channels per host to open for shared transport
        ///     client for V2 SDK inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryMaxRntbdChannels = "sharedStoreClientFactoryMaxRntbdChannels";

        /// <summary>
        ///     Name of configuration setting that specifies the number of RNTBD partitions for shared transport client for V2 SDK
        ///     inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryRntbdPartitionCount = "sharedStoreClientFactoryRntbdPartitionCount";

        /// <summary>
        ///     Name of configuration setting that specifies the number of outstanding RNTBD requests per channel for shared
        ///     transport client for V2 SDK inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryMaxRequestsPerRntbdChannel =
            "sharedStoreClientFactoryMaxRequestsPerRntbdChannel";

        /// <summary>
        ///     Name of configuration setting that specifies the amount of time in seconds to detect receive loop hanging for
        ///     shared transport client for V2 SDK inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryRntbdReceiveHangDetectionTimeSeconds =
            "sharedStoreClientFactoryRntbdReceiveHangDetectionTimeSeconds";

        /// <summary>
        ///     Name of configuration setting that specifies the amount of time in seconds to detect send loop hanging for shared
        ///     transport client for V2 SDK inside compute gateway process.
        /// </summary>
        public const string SharedStoreClientFactoryRntbdSendHangDetectionTimeSeconds =
            "sharedStoreClientFactoryRntbdSendHangDetectionTimeSeconds";
    }
}