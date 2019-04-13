//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    internal sealed class MemoryResourceMonitor : PerfCounterResourceMonitor
    {
        public MemoryResourceMonitor()
            : base("Memory", "Available MBytes")
        {
            this.Name = MetricName;
        }

        protected override float? GetCurrentValue()
        {
            // Convert from AvailableMBytes to %MemoryInUse
            var megabytesAvailable = base.GetCurrentValue().Value;
            var megabytesInUse = TotalPhysicalMemoryMBytes - megabytesAvailable;
            this.percentMemoryInUse = megabytesInUse / TotalPhysicalMemoryMBytes * 100;
            return this.percentMemoryInUse;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public MEMORYSTATUSEX()
            {
                this.DwLength = (uint) Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern bool GlobalMemoryStatusEx([In] [Out] MEMORYSTATUSEX lpBuffer);
            #pragma warning disable SA1401 // Fields should be private
            public uint DwLength;
            public uint DwMemoryLoad;
            public ulong UllTotalPhys;
            public ulong UllAvailPhys;
            public ulong UllTotalPageFile;
            public ulong UllAvailPageFile;
            public ulong UllTotalVirtual;
            public ulong UllAvailVirtual;
            public ulong UllAvailExtendedVirtual;
            #pragma warning restore SA1401 // Fields should be private
        }

        private const int BToMb = 1024 * 1024;
        private const string MetricName = "\\Memory\\% Physical MemoryInUse";
        private static readonly ulong TotalPhysicalMemoryMBytes;
        private float percentMemoryInUse;

        static MemoryResourceMonitor()
        {
            // MEMORYSTATUSEX and associated code come from Microsoft.VisualBasic.Devices.ComputerInfo, which are not available for NET Core.
            var memoryStatus = new MEMORYSTATUSEX();
            if (!MEMORYSTATUSEX.GlobalMemoryStatusEx(memoryStatus))
            {
                #pragma warning disable CA1065 // Do not raise exceptions in unexpected locations: status quo
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DiagnosticInfo_Memory");
                #pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
            }

            TotalPhysicalMemoryMBytes = memoryStatus.UllTotalPhys / BToMb;
        }
    }
}