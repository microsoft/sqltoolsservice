//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    [Flags]
    public enum ScriptExecutionResult
    {
        Success = 0x1,
        Failure = 0x2,  
        Cancel = 0x4,   
        Halted = 0x8,
        All = 0x0F
    }

}
