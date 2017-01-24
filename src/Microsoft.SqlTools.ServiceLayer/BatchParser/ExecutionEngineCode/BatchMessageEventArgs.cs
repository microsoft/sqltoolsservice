//------------------------------------------------------------------------------
// <copyright file="BatchMessageEventArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Event args for notification about non-error _message
    /// </summary>
    internal class BatchMessageEventArgs : EventArgs
    {
        #region Constructors / Destructor
                
        private BatchMessageEventArgs()
        {
        }

        internal BatchMessageEventArgs(String msg)
            : this(string.Empty, msg)
        {            
        }

        internal BatchMessageEventArgs(String detailedMsg, String msg) : this(detailedMsg, msg, null)
        {
        }
        internal BatchMessageEventArgs(String detailedMsg, String msg, SqlError error)
        {
            _message = msg;
            _detailedMessage = detailedMsg;
            _error = error;
        }
        #endregion

        #region Public properties

        public String Message
        {
            get
            {
                return _message;
            }
        }

        public String DetailedMessage
        {
            get
            {
                return _detailedMessage;
            }
        }

        public SqlError Error { get { return _error; } }
        #endregion

        #region Private fields
        private readonly String _message = String.Empty;
        private readonly String _detailedMessage = String.Empty;
        private readonly SqlError _error;
        #endregion
    }
}
