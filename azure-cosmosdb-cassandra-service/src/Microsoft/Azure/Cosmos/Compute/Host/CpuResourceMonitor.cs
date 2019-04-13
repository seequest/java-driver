//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    internal sealed class CpuResourceMonitor : PerfCounterResourceMonitor
    {
        public CpuResourceMonitor()
            : base("Processor", "% Processor Time", "_Total")
        { }
    }
}