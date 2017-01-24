//------------------------------------------------------------------------------
// <copyright file="ScriptExecutionFinishedEventArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
