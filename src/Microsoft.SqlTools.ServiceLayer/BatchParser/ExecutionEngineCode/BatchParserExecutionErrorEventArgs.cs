//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class BatchParserExecutionErrorEventArgs : BatchErrorEventArgs
    {
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

        #region Private fields
        private readonly ScriptMessageType messageType;
        #endregion
    }
}
