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
        private PerformanceCounter performanceCounter;
        private LoadMetric currentLoad;

        protected PerfCounterResourceMonitor(string categoryName, string counterName)
            : this(categoryName, counterName, null)
        {
        }

        protected PerfCounterResourceMonitor(string categoryName, string counterName, string instanceName)
        {
            this.categoryName = categoryName;
            this.counterName = counterName;
            this.instanceName = instanceName;
            this.Name = string.Format(CultureInfo.InvariantCulture, "\\{0}\\{1}", this.categoryName, this.counterName);
        }

        public LoadMetric CurrentLoad
        {
            get
            {
                float? value = this.GetCurrentValue();
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

        protected string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }

        public void Dispose()
        {
            this.Dispose(true);
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
                    PerformanceCounterCategory category = new PerformanceCounterCategory(this.categoryName);
                    if (category.CounterExists(this.counterName))
                    {
                        if (string.IsNullOrEmpty(this.instanceName))
                        {
                            this.performanceCounter = new PerformanceCounter(this.categoryName, this.counterName);
                        }
                        else if (category.InstanceExists(this.instanceName))
                        {
                            this.performanceCounter = new PerformanceCounter(this.categoryName, this.counterName, this.instanceName);
                        }
                        else
                        {
                            // Category and Counter exist but Instance does not yet
                        }
                    }
                }
            }

            return this.performanceCounter != null;
        }
    }
}
