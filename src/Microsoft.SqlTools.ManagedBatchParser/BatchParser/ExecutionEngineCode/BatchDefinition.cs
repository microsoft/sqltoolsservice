// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    ///<summary>
    /// Class to get text from the BatchParser and convert them into batches
    ///</summary>
    public class BatchDefinition
    {

        /// <summary>
        /// Constructor method for a BatchDefinition
        /// </summary>
        public BatchDefinition(string batchText, int startLine, int endLine, int startColumn, int endColumn, int executionCount, SqlCmdCommand command)
        {
            BatchText = batchText;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
            // set the batch execution count, with min value of 1
            BatchExecutionCount = executionCount > 0 ? executionCount : 1;
            SqlCmdCommand = command;
        }
        
        /// <summary>
        /// Get starting line of the BatchDefinition
        /// </summary>
        public int StartLine
        {
            get; private set;
        }

        /// <summary>
        /// Get ending line of the BatchDefinition
        /// </summary>
        public int EndLine
        {
            get; private set;
        }

        /// <summary>
        /// Get starting column of the BatchDefinition
        /// </summary>
        public int StartColumn
        {
            get; private set;
        }

        /// <summary>
        /// Get ending column of the BatchDefinition
        /// </summary>
        public int EndColumn
        {
            get; private set;
        }

        /// <summary>
        /// Get batch text associated with the BatchDefinition
        /// </summary>
        public string BatchText
        {
            get; private set;
        }

        public SqlCmdCommand SqlCmdCommand
        {
            get; private set;
        }

        /// <summary>
        /// Get number of times to execute this batch
        /// </summary>
        public int BatchExecutionCount
        {
            get; private set;
        }
    }
}
