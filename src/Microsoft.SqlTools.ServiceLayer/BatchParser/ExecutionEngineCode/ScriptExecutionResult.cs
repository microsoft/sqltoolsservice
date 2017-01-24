//------------------------------------------------------------------------------
// <copyright file="ScriptExecutionResult.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    [Flags]
    internal enum ScriptExecutionResult
    {
        Success = 0x1,
        Failure = 0x2,  
        Cancel = 0x4,   
        Halted = 0x8,
        All = 0x0F
    }

}
