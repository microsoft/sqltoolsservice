//------------------------------------------------------------------------------
// <copyright file="BatchParserExecutionErrorEventArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class BatchParserExecutionErrorEventArgs : BatchErrorEventArgs
    {
        public BatchParserExecutionErrorEventArgs(String errorLine, String message, ScriptMessageType messageType)
            : base(errorLine, message, null)
        {
            _messageType = messageType;
        }

        public ScriptMessageType MessageType
        {
            get
            {
                return _messageType;
            }
        }

        #region Private fields
        private readonly ScriptMessageType _messageType;
        #endregion
    }
}
