//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.CosmosDB;
    using Microsoft.Azure.CosmosDB.Diagnostics;
    using Microsoft.Azure.Documents;
    using Microsoft.Extensions.DependencyInjection;

    internal sealed class NodeResourceGovernor : IResourceGovernor
    {
        // evaluate the node resource health once every 10 seconds.
        private const int ResourceGovernorEvalInterval = 10 * 1000;
        private readonly List<ResourceHeatMapEntry> resourceHeatMap;

        private readonly IList<IResourceMonitor> resourceMonitors;
        private readonly TimerPool timerPool;

        public NodeResourceGovernor(IServiceProvider serviceProvider)
        {
            this.timerPool = serviceProvider.GetService<TimerPool>();
            this.resourceMonitors = new List<IResourceMonitor>
            {
                new CpuResourceMonitor(),
                new MemoryResourceMonitor()
            };

            this.resourceHeatMap = new List<ResourceHeatMapEntry>
            {
                new ResourceHeatMapEntry {State = NodeHealthState.Normal, MinValue = 0, MaxValue = 85},
                new ResourceHeatMapEntry {State = NodeHealthState.Warm, MinValue = 86, MaxValue = 95},
                new ResourceHeatMapEntry {State = NodeHealthState.Normal, MinValue = 96, MaxValue = 100}
            };

            this.EnsureTimer();
        }

        public NodeHealthState HealthState { get; private set; }

        public event EventHandler<NodeHealthStateEventArgs> NodeHealthChangeEvent;

        private void Run()
        {
            try
            {
                var previousState = this.HealthState;
                var state = NodeHealthState.Normal;
                foreach (var resourceMonitor in this.resourceMonitors)
                {
                    var currentResourceUsage = resourceMonitor.CurrentLoad.Value;
                    foreach (var entry in this.resourceHeatMap)
                    {
                        if (currentResourceUsage > entry.MinValue && currentResourceUsage < entry.MaxValue &&
                            state < entry.State)
                        {
                            state = entry.State;
                        }
                    }
                }

                this.HealthState = state;
                var eventArgs = new NodeHealthStateEventArgs
                {
                    CurrentState = this.HealthState,
                    PreviousState = previousState
                };

                if (previousState != this.HealthState && this.NodeHealthChangeEvent != null)
                {
                    if (this.HealthState == NodeHealthState.Hot)
                    {
                        CosmosDBTrace.TraceError("NodeHealth state changed from {0} to {1}", previousState,
                            this.HealthState);
                    }
                    else if (this.HealthState == NodeHealthState.Warm)
                    {
                        CosmosDBTrace.TraceWarning("NodeHealth state changed from {0} to {1}", previousState,
                            this.HealthState);
                    }

                    this.NodeHealthChangeEvent.Invoke(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                CosmosDBTrace.TraceError("Unexpted exception in Noderesource governor runasyn {0}", ex);
            }
            finally
            {
                this.EnsureTimer();
            }
        }

        private void EnsureTimer()
        {
            try
            {
                this.timerPool.GetPooledTimer(ResourceGovernorEvalInterval).StartTimerAsync().ContinueWith(
                    task =>
                    {
                        if (task.IsFaulted)
                        {
                            CosmosDBTrace.TraceError("Timer failed with exception {0}", task.Exception);
                        }
                        else
                        {
                            this.OnTimer();
                        }
                    }).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                CosmosDBTrace.TraceError("Timer pool is disposed, process is shutting down");
            }
        }

        private void OnTimer()
        {
            this.Run();
        }

        private sealed class ResourceHeatMapEntry
        {
            public NodeHealthState State { get; set; }

            public int MinValue { get; set; }

            public int MaxValue { get; set; }
        }
    }
}