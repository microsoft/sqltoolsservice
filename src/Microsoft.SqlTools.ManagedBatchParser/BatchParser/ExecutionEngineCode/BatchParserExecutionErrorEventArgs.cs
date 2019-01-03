//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Class associated with batch parser execution errors
    /// </summary>
    public class BatchParserExecutionErrorEventArgs : BatchErrorEventArgs
    {
        private readonly ScriptMessageType messageType;

        /// <summary>
        /// Constructor method for BatchParserExecutionErrorEventArgs class
        /// </summary>
        public BatchParserExecutionErrorEventArgs(string errorLine, string message, ScriptMessageType messageType)
            : base(errorLine, message, null)
        {
            this.messageType = messageType;
        }

        public ScriptMessageType MessageType
        {
            get
            {
                return messageType;
            }
        }

    }
}
