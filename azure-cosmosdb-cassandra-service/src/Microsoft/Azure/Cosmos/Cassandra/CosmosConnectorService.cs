//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Azure.CosmosDB;

    internal sealed class CosmosConnectorService
    {
        private readonly CosmosConnector connector;
        private readonly ICosmosDBDataProvider dataProvider;

        public CosmosConnectorService(
            ICosmosDBConfigProvider configurationProvider,
            ICosmosDBDataProvider dataProvider = null,
            IServiceProvider hostServiceProvider = null)
        {
            this.ConfigurationProvider = configurationProvider ?? new CosmosConnectorConfigurationProvider();
            this.dataProvider = dataProvider ?? new CosmosConnectorDataProvider();

            ICosmosDBHostRuntimeContext hostRuntimeContext = new CosmosConnectorHostRuntimeContext();

            if (hostServiceProvider == null)
            {
                this.connector = new CosmosConnector(this.ConfigurationProvider, this.dataProvider,
                    null,
                    hostRuntimeContext);
            }
            else
            {
                this.connector =
                    new CosmosConnector(this.ConfigurationProvider, hostServiceProvider, hostRuntimeContext);
            }

            UpdateHostsFile();
        }

        private ICosmosDBConfigProvider ConfigurationProvider { get; }

        public IServiceProvider HostServiceProvider => this.connector.HostServiceProvider;

        public void Start()
        {
            this.connector.OpenAsync().Wait();
        }

        public void Close()
        {
            this.connector.CloseAsync().Wait();
        }

        public Task CloseAsync()
        {
            return this.connector.CloseAsync();
        }

        private static void UpdateHostsFile()
        {
            var hostsFilePath =
                Path.Combine(Environment.GetEnvironmentVariable("windir"), @"system32\drivers\etc\hosts");
            var hostsFileContent = File.ReadAllText(hostsFilePath);

            if (!hostsFileContent.Contains("localhost.table"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"127.0.0.1 " + "localhost.table"});
            }

            if (!hostsFileContent.Contains("localhost.sql"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"127.0.0.1 " + "localhost.sql"});
            }

            if (!hostsFileContent.Contains("localhost.gremlin"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"127.0.0.1 " + "localhost.gremlin"});
            }

            if (!hostsFileContent.Contains("localhost.query"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"127.0.0.1 " + "localhost.query"});
            }

            if (!hostsFileContent.Contains("localhost.echo"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"127.0.0.1 " + "localhost.echo"});
            }

            // IP V6 entries
            if (!hostsFileContent.Contains("localhostv6.table"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"::1 " + "localhostv6.table"});
            }

            if (!hostsFileContent.Contains("localhostv6.sql"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"::1 " + "localhostv6.sql"});
            }

            if (!hostsFileContent.Contains("localhostv6.gremlin"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"::1 " + "localhostv6.gremlin"});
            }

            if (!hostsFileContent.Contains("localhostv6.query"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"::1 " + "localhostv6.query"});
            }

            if (!hostsFileContent.Contains("localhostv6.echo"))
            {
                File.AppendAllLines(hostsFilePath, new[] {"::1 " + "localhostv6.echo"});
            }
        }
    }
}