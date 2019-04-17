namespace Microsoft.Azure.Cosmos.Posix.Diagnostics
{
    using System;
    using global::Microsoft.Azure.CosmosDB.Diagnostics;

    public class ComputeEventSource : IComputeEventSource
    {
        public static IComputeEventSource Log = new ComputeEventSource();

        public bool InitializeMdmMonitoring(string monitoringAccount, string monitoringNamespace, string tenant, string role,
            string roleInstance)
        {
            throw new NotImplementedException();
        }

        public void ComputeGatewayInitMetric(string processName, string assemblyName, string assemblyVersion)
        {
            throw new NotImplementedException();
        }

        public void ComputeRequest(Guid activityId, string backendActivityId, string subscriptionId, string globalDatabaseAccountName,
            string regionalDatabaseAccountName, string region, string databaseName, string collectionName, short statusCode,
            InternalStatusCode internalStatusCode, int apiSpecificErrorCode, string apiType, short networkBucket,
            string operationName, string resourceName, string userAgent, string versionHeader, string address,
            double durationInMilliseconds, long requestLength, long responseLength, double customerComputeRequestUnits,
            double customerStorageRequestUnits, double customerTotalRequestUnits, double logicalComputeRequestUnits,
            double physicalComputeRequestUnits, long bufferReadElapsedTicks, long formatRequestElapsedTicks,
            long interopAndSdkElapsedTicks, int requestItemsImpacted, string apiConsistency, bool hasContinuation,
            int responseItemCount, long interopElapsedTicks, long sdkElapsedTicks, long transportAndBackendElapsedTicks,
            long memoryAllocatedInBytes)
        {
            throw new NotImplementedException();
        }

        public void ConnectionClose(string subscriptionId, string globalDatabaseAccountName, string region,
            string connectionCloseReason, string apiType, int nativeErrorCode, double durationInMilliseconds)
        {
            throw new NotImplementedException();
        }

        public void ComputePhysicalCharge(string label, long clockCycles, string subscriptionId, string globalDatabaseAccountName,
            string regionalDatabaseAccountName, string region, string databaseName, string collectionName, short statusCode,
            string apiType, string operationName, string resourceName, bool isRequest)
        {
            throw new NotImplementedException();
        }

        public void LogComputeRequestLatency(string activityId, long bufferReadElapsedTicks, long formatRequestElapsedTicks,
            long interopAndSdkElapsedTicks, long handleResponseElapsedTicks)
        {
            throw new NotImplementedException();
        }

        public void ComputeRequestCharge(string subscriptionId, string databaseAccount, string operationType, string databaseRid,
            string collectionRid, string statusCode, string globalDatabaseAccountName, string region, string networkBucket,
            string partitionId, int second, string isBurstingRequest, string subStatusCode, string resourceType,
            string partitionKeyRangeId, string statusCodePrefix, string collectionName, string databaseName,
            DateTime serverTimeStampUtc, long chargeValue)
        {
            throw new NotImplementedException();
        }

        public void LogComputePhysicalResources(string subscriptionId, string globalDatabaseAccountName, long connectionOpenCount = 0,
            long connectionCloseCount = 0)
        {
            throw new NotImplementedException();
        }

        public void LogComputeResourceGovernanceMetrics(string subscriptionId, string globalDatabaseAccountName, string databaseName,
            string collectionName, long cpuClockCyclesConsumed, long memoryAllocatedInBytes, double logicalRUsConsumedInCompute,
            double logicalRUsConsumedInBackend, string cpuSlaViolation, string memorySlaViolation,
            string logicalRUsSlaViolation, long timeInTaskSchedulerInTicks, long asyncTaskCount, long inlineTaskCount,
            long computeThrottledTimeInTicks, long backendThrottledTimeInTicks, long computeThrottledRequestsCount,
            long backendThrottledRequestsCount, long timeInThreadPoolInTicks)
        {
            throw new NotImplementedException();
        }
    }
}