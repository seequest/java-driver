// <copyright file="CosmosDbServiceUriTemplateEnumerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Cosmos.Compute.Host
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.CosmosDB;
    using Microsoft.Azure.CosmosDB.UriMatch;

    /// <inheritdoc />
    internal sealed class CosmosDbServiceUriTemplateEnumerator : ICosmosDBServiceUriTemplateEnumerator
    {
        private readonly CosmosDBServiceResolver serviceResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbServiceUriTemplateEnumerator"/> class
        /// </summary>
        public CosmosDbServiceUriTemplateEnumerator(CosmosDBServiceResolver serviceResolver)
        {
            this.serviceResolver = serviceResolver;
        }

        /// <inheritdoc />
        public IEnumerable<ICosmosDBUriTemplate> GetRegisteredUriTemplates(string transportScheme)
        {
            foreach (ICosmosDBUriTemplate uriTemplate in this.serviceResolver.GetRegisteredUriTemplates())
            {
                if (string.Equals(
                    uriTemplate.Scheme,
                    transportScheme,
                    StringComparison.OrdinalIgnoreCase))
                {
                    yield return uriTemplate;
                }
            }
        }
    }
}