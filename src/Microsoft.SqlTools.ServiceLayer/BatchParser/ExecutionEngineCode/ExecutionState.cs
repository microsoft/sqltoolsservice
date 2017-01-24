//------------------------------------------------------------------------------
// <copyright file="ExecutionState.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal enum ExecutionState
    {
        Initial, 
        Executing, 
        ExecutingBatch, 
        Cancelling, 
        Discarded
    }
}
