//------------------------------------------------------------------------------
// <copyright file="SqlServerError.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// Represents an error produced by SQL Server database schema provider
    /// </summary>
    [Serializable]
    internal class SqlServerError : DataSchemaError
    {
        private const string SqlServerPrefix = "SQL";
        private const string DefaultHelpKeyword = "vs.teamsystem.datatools.DefaultErrorMessageHelp";

        public SqlServerError(string message, string document, ErrorSeverity severity)
            : this(message, null, document, 0, 0, Constants.UndefinedErrorCode, severity)
        {
        }

        public SqlServerError(string message, string document, int errorCode, ErrorSeverity severity)
            : this(message, null, document, 0, 0, errorCode, severity)
        {
        }

        public SqlServerError(Exception exception, string document, int errorCode, ErrorSeverity severity)
            : this(exception, document, 0, 0, errorCode, severity)
        {
        }

        public SqlServerError(string message, string document, int line, int column, ErrorSeverity severity)
            : this(message, null, document, line, column, Constants.UndefinedErrorCode, severity) 
        {
        }

        public SqlServerError(
            Exception exception,
            string document,
            int line,
            int column,
            int errorCode,
            ErrorSeverity severity) :
            this(exception.Message, exception, document, line, column, errorCode, severity)
        {
        }

        public SqlServerError(
            string message,
            string document,
            int line,
            int column,
            int errorCode,
            ErrorSeverity severity) :
            this(message, null, document, line, column, errorCode, severity)
        {
        }

        public SqlServerError(
            string message,
            Exception exception,
            string document,
            int line,
            int column,
            int errorCode,
            ErrorSeverity severity) :
            base(message, exception, document, line, column, SqlServerPrefix, errorCode, severity)
        {
            this.HelpKeyword = DefaultHelpKeyword;
        }
    }
}
