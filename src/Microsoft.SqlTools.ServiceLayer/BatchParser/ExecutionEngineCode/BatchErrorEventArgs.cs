//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Error totalAffectedRows for a Batch
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
        internal BatchErrorEventArgs(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Constructor with exception and no description
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        internal BatchErrorEventArgs(string message, Exception ex)
            : this(message, string.Empty, ex)
        {
        }

        /// <summary>
        /// Constructor with message and description
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="ex"></param>
        internal BatchErrorEventArgs(string message, string description, Exception ex)
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
            this.error = error;
        }

        /// <summary>
        /// Constructor with message, description, textspan and line number
        /// </summary>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="line"></param>
        /// <param name="textSpan"></param>
        internal BatchErrorEventArgs(string message, string description, int line, TextSpan textSpan, Exception ex)
        {
            Init(message, description, line, textSpan, ex);
        }

        private void Init(string message, string description, int line, TextSpan textSpan, Exception ex)
        {
            this.message = message;
            this.description = description;
            this.line = line;
            this.textSpan = textSpan;
            exception = ex;
        }

        #endregion

        #region Public properties

        public string Message
        {
            get
            {
                return message;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public int Line
        {
            get 
            { 
                return line; 
            }
        }

        public TextSpan TextSpan
        {
            get 
            { 
                return textSpan; 
            }
        }

        public Exception Exception
        {
            get
            {
                return exception;
            }
        }

        public SqlError Error
        {
            get { return error; }
        }


        #endregion

        #region Private Fields
        private string message = string.Empty;
        private string description = string.Empty;
        private int line = -1;
        private TextSpan textSpan;
        private Exception exception;
        private SqlError error;
        #endregion
    }
}
