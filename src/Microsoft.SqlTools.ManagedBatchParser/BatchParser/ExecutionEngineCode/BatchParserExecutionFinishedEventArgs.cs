//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Class associated with batch parser execution finished event
    /// </summary>
    public class BatchParserExecutionFinishedEventArgs : EventArgs
    {
        
        private readonly Batch batch = null;
        private readonly ScriptExecutionResult result;
        private readonly SqlCmdCommand sqlCmdCommand;

        private BatchParserExecutionFinishedEventArgs() 
        {
        }

        /// <summary>
        /// Constructor method for the class
        /// </summary>
        public BatchParserExecutionFinishedEventArgs(ScriptExecutionResult batchResult, Batch batch, SqlCmdCommand sqlCmdCommand)            
        {
            this.batch = batch;
            result = batchResult;
            this.sqlCmdCommand = sqlCmdCommand;
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

        public SqlCmdCommand SqlCmdCommand
        {
            get
            {
                return sqlCmdCommand;
            }
        }
    }
}
