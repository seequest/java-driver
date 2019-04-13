//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using CosmosDB;

    internal sealed class CassandraService
    {
        private readonly CassandraCosmosConnector _connector;
        private readonly ICosmosDBDataProvider dataProvider;

        public CassandraService(ICosmosDBConfigProvider configurationProvider,
            ICosmosDBDataProvider dataProvider = null,
            IServiceProvider hostServiceProvider = null)
        {
            this.ConfigurationProvider = configurationProvider ?? new ServiceConfigurationProvider();
            this.dataProvider = dataProvider ?? new ServiceDataProvider();

            ICosmosDBHostRuntimeContext hostRuntimeContext = new ServiceHostRuntimeContext();

            if (hostServiceProvider == null)
            {
                this._connector = new CassandraCosmosConnector(this.ConfigurationProvider, this.dataProvider,
                    stateManagerFactory: null,
                    runtimeContext: hostRuntimeContext);
            }
            else
            {
                this._connector =
                    new CassandraCosmosConnector(this.ConfigurationProvider, hostServiceProvider, hostRuntimeContext);
            }

            this.UpdateHostsFile();
        }

        internal ICosmosDBConfigProvider ConfigurationProvider { get; }

        internal IServiceProvider HostServiceProvider => this._connector.HostServiceProvider;

        public void Start()
        {
            this._connector.OpenAsync().Wait();
        }

        public void Close()
        {
            this._connector.CloseAsync().Wait();
        }

        public Task CloseAsync()
        {
            return this._connector.CloseAsync();
        }

        private void UpdateHostsFile()
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