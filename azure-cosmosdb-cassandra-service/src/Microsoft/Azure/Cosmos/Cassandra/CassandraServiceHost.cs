﻿//------------------------------------------------------------
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
    using AspNetCore;
    using AspNetCore.Hosting;

    public static class ServiceHost
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
                            Path.Combine(Path.GetDirectoryName(typeof(ServiceHost).Assembly.Location),
                                AppDomain.CurrentDomain.FriendlyName), ".config")
                };

                var configurations = new List<Configuration>
                {
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None),
                    ConfigurationManager.OpenMappedExeConfiguration(configurationFileMap, ConfigurationUserLevel.None)
                };

                var computeService = new CassandraService(new ServiceConfigurationProvider(configurations));
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
            catch (Exception ex)
            {
                Trace.WriteLine($"Unhandled Exception in console service host {ex}");
            }

            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>();
        }
    }
}