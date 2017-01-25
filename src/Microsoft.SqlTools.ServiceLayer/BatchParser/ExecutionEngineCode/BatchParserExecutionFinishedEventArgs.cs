//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
            this.batch = batch;
            result = batchResult;
        }

        public Batch Batch
        {
            get
            {
                return batch;
            }
        }

        public ScriptExecutionResult ExecutionResult
        {
            get
            {
                return result;
            }
        }

        #region Private members
        private readonly Batch batch = null;
        private readonly ScriptExecutionResult result;
        #endregion
    }
}
