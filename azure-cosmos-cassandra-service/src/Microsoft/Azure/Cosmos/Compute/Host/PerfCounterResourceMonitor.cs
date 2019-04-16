//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Azure.CosmosDB;

    internal abstract class PerfCounterResourceMonitor : IResourceMonitor
    {
        private readonly string categoryName;
        private readonly string counterName;
        private readonly string instanceName;
        private LoadMetric currentLoad;
        private PerformanceCounter performanceCounter;

        protected PerfCounterResourceMonitor(string categoryName, string counterName)
            : this(categoryName, counterName, null)
        { }

        protected PerfCounterResourceMonitor(string categoryName, string counterName, string instanceName)
        {
            this.categoryName = categoryName;
            this.counterName = counterName;
            this.instanceName = instanceName;
            this.Name = string.Format(CultureInfo.InvariantCulture, "\\{0}\\{1}", this.categoryName, this.counterName);
        }

        protected string Name { get; set; }

        public LoadMetric CurrentLoad
        {
            get
            {
                var value = this.GetCurrentValue();
                if (value.HasValue)
                {
                    if (this.currentLoad == null)
                    {
                        this.currentLoad = new LoadMetric(this.Name, value.Value);
                    }
                    else
                    {
                        this.currentLoad.Value = value.Value;
                    }
                }

                return this.currentLoad;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        public override string ToString()
        {
            return this.Name;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.performanceCounter != null)
                {
                    this.performanceCounter.Dispose();
                }
            }
        }

        protected virtual float? GetCurrentValue()
        {
            if (this.TryInitializeCounter())
            {
                return this.performanceCounter.NextValue();
            }

            return null;
        }

        private bool TryInitializeCounter()
        {
            if (this.performanceCounter == null)
            {
                if (PerformanceCounterCategory.Exists(this.categoryName))
                {
                    var category = new PerformanceCounterCategory(this.categoryName);
                    if (category.CounterExists(this.counterName))
                    {
                        if (string.IsNullOrEmpty(this.instanceName))
                        {
                            this.performanceCounter = new PerformanceCounter(this.categoryName, this.counterName);
                        }
                        else if (category.InstanceExists(this.instanceName))
                        {
                            this.performanceCounter =
                                new PerformanceCounter(this.categoryName, this.counterName, this.instanceName);
                        }
                    }
                }
            }

            return this.performanceCounter != null;
        }
    }
}