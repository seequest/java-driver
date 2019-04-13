//---------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//---------------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Cassandra
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.CosmosDB;

    /// <summary>
    ///     Cassandra service configuration provider that works through application configuration file.
    /// </summary>
    internal sealed class CosmosConnectorConfigurationProvider : ICosmosDBConfigProvider
    {
        private static readonly HashSet<string> DocumentClientKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "documentServiceApiEndpoint"
            };

        private readonly ConcurrentDictionary<string, string> addedKeys = new ConcurrentDictionary<string, string>();

        private readonly List<KeyValueConfigurationCollection> keyValueConfigurations =
            new List<KeyValueConfigurationCollection>();

        private readonly string scopeName;
        private ICosmosDBConfigProvider parent;

        public CosmosConnectorConfigurationProvider()
        {
            var currentExeConfiguration =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            this.keyValueConfigurations.Add(currentExeConfiguration.AppSettings.Settings);
        }

        public CosmosConnectorConfigurationProvider(List<Configuration> exeConfiguration)
        {
            this.keyValueConfigurations.AddRange(exeConfiguration.Select(config => config.AppSettings.Settings));
        }

        private CosmosConnectorConfigurationProvider(string scopeName, ICosmosDBConfigProvider parent,
            List<KeyValueConfigurationCollection> keyValueConfigurations,
            ConcurrentDictionary<string, string> addedKeys)
        {
            this.scopeName = scopeName;
            this.parent = parent;
            this.keyValueConfigurations = keyValueConfigurations;
            this.addedKeys = addedKeys;
        }

        private static string DocumentClientUri { get; set; }

        public Task<ICosmosDBConfigProvider> GetConfigurationGroupAsync(string configurationGroupName)
        {
            return Task.FromResult<ICosmosDBConfigProvider>(
                new CosmosConnectorConfigurationProvider(configurationGroupName, this, this.keyValueConfigurations,
                    this.addedKeys));
        }

        public bool TryGetValue(string key, out string value)
        {
            var lookupKey = string.IsNullOrEmpty(this.scopeName) ? key : this.scopeName + "::" + key;

            if (this.addedKeys.TryGetValue(lookupKey, out value))
            {
                return true;
            }

            foreach (var keyValueConfigurations in this.keyValueConfigurations)
            {
                var keyValue = keyValueConfigurations[lookupKey];
                value = keyValue?.Value;

                if (DocumentClientKeys.Contains(key)
                    && !string.IsNullOrEmpty(DocumentClientUri)
                    && !string.IsNullOrEmpty(value))
                {
                    value = DocumentClientUri;
                }

                if (keyValue != null)
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        public void AddKeyValue(string key, string value)
        {
            var lookupKey = string.IsNullOrEmpty(this.scopeName) ? key : this.scopeName + "::" + key;
            this.addedKeys[lookupKey] = value;
        }

        public void RemoveKey(string key)
        {
            var lookupKey = string.IsNullOrEmpty(this.scopeName) ? key : this.scopeName + "::" + key;
            this.keyValueConfigurations.ForEach(setting => setting.Remove(lookupKey));
            this.addedKeys.TryRemove(lookupKey, out var unused);
        }

        public void RemoveAddedKey(string key)
        {
            var lookupKey = string.IsNullOrEmpty(this.scopeName) ? key : this.scopeName + "::" + key;
            this.addedKeys.TryRemove(lookupKey, out var unused);
        }
    }
}