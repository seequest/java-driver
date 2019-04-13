//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.CosmosDB.StateManagement
{
    using System.Net;

    /// <summary>
    /// StateManagerResponse class reflects the response from state manager.
    /// </summary>
    internal sealed class StateManagerResponse
    {
        public StateManagerResponse(HttpStatusCode statusCode, StateItem item)
        {
            this.StatusCode = statusCode;
            this.Item = item;
        }

        /// <summary>
        /// Status code are equivalent HttpStatusCode that conveys the caller on high level info of the response so that its easier to make decisions at caller level
        /// instead of being dependant on catching excpetions which consume lot of CPU.
        /// For instance was it a success  or Conflict (key already existed) etc.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get; set;
        }

        /// <summary>
        /// Item is the payload with which the caller made a add/update/upsert request. In response context, it is more useful for get calls where an item is retrieved.
        /// </summary>
        public StateItem Item
        {
            get; set;
        }
    }
}
