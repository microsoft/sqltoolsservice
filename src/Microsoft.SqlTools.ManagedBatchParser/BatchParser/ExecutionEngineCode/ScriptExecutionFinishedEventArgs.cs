//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class ScriptExecutionFinishedEventArgs : EventArgs
    {   
        internal ScriptExecutionFinishedEventArgs(ScriptExecutionResult result)
        {
            ExecutionResult = result;
        }

        public ScriptExecutionResult ExecutionResult
        {
            private set;
            get;
        }

        public bool Disposing
        {
            get;
            set;
        }
    }
}
