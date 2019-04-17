//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    internal sealed class MemoryResourceMonitor : PerfCounterResourceMonitor
    {
        private const int BToMb = 1024 * 1024;
        private const string MetricName = "\\Memory\\% Physical MemoryInUse";
        private static readonly ulong TotalPhysicalMemoryMBytes;
        
        private float percentMemoryInUse;

        #if Windows_NT
        
        static MemoryResourceMonitor()
        {
            // MEMORYSTATUSEX and associated code come from Microsoft.VisualBasic.Devices.ComputerInfo, which are not 
            // available for .NET Core
            var memoryStatus = new MEMORYSTATUSEX();
            if (!MEMORYSTATUSEX.GlobalMemoryStatusEx(memoryStatus))
            {
                #pragma warning disable CA1065  // Do not raise exceptions in unexpected locations: status quo
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DiagnosticInfo_Memory");
                #pragma warning restore CA1065  // Do not raise exceptions in unexpected locations
            }

            TotalPhysicalMemoryMBytes = memoryStatus.UllTotalPhys / BToMb;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private class MEMORYSTATUSEX
        {
            #pragma warning disable SA1401  // Fields should be private
            public uint DwLength;
            public uint DwMemoryLoad;
            public ulong UllTotalPhys;
            public ulong UllAvailPhys;
            public ulong UllTotalPageFile;
            public ulong UllAvailPageFile;
            public ulong UllTotalVirtual;
            public ulong UllAvailVirtual;
            public ulong UllAvailExtendedVirtual;
            #pragma warning restore SA1401  // Fields should be private

            public MEMORYSTATUSEX()
            {
                this.DwLength = (uint) Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GlobalMemoryStatusEx([In] [Out] MEMORYSTATUSEX lpBuffer);
        }

        #elif Darwin || Linux
        
        static MemoryResourceMonitor()
        {
            TotalPhysicalMemoryMBytes = 1;
        }

        #endif

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
    }
}
