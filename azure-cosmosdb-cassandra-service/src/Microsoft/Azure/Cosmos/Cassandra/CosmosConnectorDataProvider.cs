//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Azure.CosmosDB;

    /// <summary>
    ///     Data provider for test environment where package is co-located with binaries.
    /// </summary>
    internal sealed class CosmosConnectorDataProvider : ICosmosDBDataProvider
    {
//#pragma warning disable 67

        /// <summary>
        ///     Event that fires when there is a change in data package.
        /// </summary>
        public event EventHandler<CosmosDBDataProviderEventArgs> OnDataChanged;

//#pragma warning restore 67

        /// <summary>
        ///     Path to the data package.
        /// </summary>
        public string DataPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}