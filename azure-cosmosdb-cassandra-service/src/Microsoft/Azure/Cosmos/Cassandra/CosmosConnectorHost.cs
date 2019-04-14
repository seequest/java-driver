//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public static class CosmosConnectorHost
    {
        public static void Main(string[] args)
        {
            try
            {
                // Microsoft.Azure.CosmosDB.ServiceCommon.RuntimePerfCounters.Counters.InstallCounters();

                var configurationFileMap = new ExeConfigurationFileMap
                {
                    ExeConfigFilename =
                        Path.ChangeExtension(
                            Path.Combine(Path.GetDirectoryName(typeof(CosmosConnectorHost).Assembly.Location),
                                AppDomain.CurrentDomain.FriendlyName), ".config")
                };

                var configurations = new List<Configuration>
                {
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None),
                    ConfigurationManager.OpenMappedExeConfiguration(configurationFileMap, ConfigurationUserLevel.None)
                };

                var computeService =
                    new CosmosConnectorService(new CosmosConnectorConfigurationProvider(configurations));
                var completionTask = new TaskCompletionSource<object>();

                Console.CancelKeyPress += (sender, e) =>
                {
                    if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                    {
                        completionTask.SetResult(null);
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                };

                computeService.Start();
                Console.WriteLine("Waiting for Ctrl+C.");
                completionTask.Task.Wait();
                computeService.Close();
            }
            catch (Exception error)
            {
                Trace.WriteLine($"Unhandled Exception in console service host {error}");
            }
        }
    }
}