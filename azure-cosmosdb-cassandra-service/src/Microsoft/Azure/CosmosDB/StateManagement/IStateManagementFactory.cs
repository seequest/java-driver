//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.CosmosDB.StateManagement
{
    /// <summary>
    /// IStateManagerFactory Interface is used to give out the State Manager.
    /// </summary>
    internal interface IStateManagerFactory
    {
        IStateManager GetStateManager(string accountName);
    }
}
