//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
