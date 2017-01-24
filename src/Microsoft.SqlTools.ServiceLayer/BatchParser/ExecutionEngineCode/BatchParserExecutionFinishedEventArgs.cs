//------------------------------------------------------------------------------
// <copyright file="BatchParserExecutionFinishedEventArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class BatchParserExecutionFinishedEventArgs : EventArgs
    {
        private BatchParserExecutionFinishedEventArgs() 
        {
        }

        public BatchParserExecutionFinishedEventArgs(ScriptExecutionResult batchResult, Batch batch)            
        {
            _batch = batch;
            _result = batchResult;
        }

        public Batch Batch
        {
            get
            {
                return _batch;
            }
        }

        public ScriptExecutionResult ExecutionResult
        {
            get
            {
                return _result;
            }
        }

        #region Private members
        private readonly Batch _batch = null;
        private readonly ScriptExecutionResult _result;
        #endregion
    }
}
