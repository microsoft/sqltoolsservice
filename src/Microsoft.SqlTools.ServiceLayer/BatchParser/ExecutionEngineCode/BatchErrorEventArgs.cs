//------------------------------------------------------------------------------
// <copyright file="BatchErrorEventArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.SqlTools.ServiceLayer;
using System;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Error _totalAffectedRows for a Batch
    /// </summary>
    internal class BatchErrorEventArgs : EventArgs
    {
        #region Constructors / Destructor
                
        /// <summary>
        /// Default constructor
        /// </summary>
        private BatchErrorEventArgs()
        {
        }

        /// <summary>
        /// Constructor with message and no description
        /// </summary>
        /// <param name="message"></param>
        internal BatchErrorEventArgs(String message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Constructor with exception and no description
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        internal BatchErrorEventArgs(String message, Exception ex)
            : this(message, string.Empty, ex)
        {
        }

        /// <summary>
        /// Constructor with message and description
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="ex"></param>
        internal BatchErrorEventArgs(String message, String description, Exception ex)
            : this(message, description, -1, new TextSpan(), ex)
        {
        }

        internal BatchErrorEventArgs(string message, SqlError error, TextSpan textSpan, Exception ex)
        {
            string desc = error != null ? error.Message : null;
            if (error.Number == 7202)
            {
                desc += " \r\n" + SR.TroubleshootingAssistanceMessage;
            }

            int lineNumber = error != null ? error.LineNumber : -1;
            Init(message, desc, lineNumber, textSpan, ex);
            _error = error;
        }

        /// <summary>
        /// Constructor with message, description, textspan and line number
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="line"></param>
        /// <param name="textSpan"></param>
        internal BatchErrorEventArgs(String message, String description, int line, TextSpan textSpan, Exception ex)
        {
            Init(message, description, line, textSpan, ex);
        }

        private void Init(String message, String description, int line, TextSpan textSpan, Exception ex)
        {
            _message = message;
            _description = description;
            _line = line;
            _textSpan = textSpan;
            _exception = ex;
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

        public String Description
        {
            get
            {
                return _description;
            }
        }

        public int Line
        {
            get 
            { 
                return _line; 
            }
        }

        public TextSpan TextSpan
        {
            get 
            { 
                return _textSpan; 
            }
        }

        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }

        public SqlError Error
        {
            get { return _error; }
        }


        #endregion

        #region Private Fields
        private string _message = string.Empty;
        private string _description = string.Empty;
        private int _line = -1;
        private TextSpan _textSpan;
        private Exception _exception;
        private SqlError _error;
        #endregion
    }
}
