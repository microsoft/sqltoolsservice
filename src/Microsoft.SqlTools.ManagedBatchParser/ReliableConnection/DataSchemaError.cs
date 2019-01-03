//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// This class is used to encapsulate all the information needed by the DataSchemaErrorTaskService to create a corresponding entry in the Visual Studio Error List.
    /// A component should add this Error Object to the <see cref="ErrorManager"/> for such purpose.
    /// Errors and their children are expected to be thread-safe.  Ideally, this means that
    /// the objects are just data-transfer-objects initialized during construction.
    /// </summary>
    [Serializable]
    public class DataSchemaError
    {
        internal const string DefaultPrefix = "SQL";
        private const int MaxErrorCode = 99999;
        protected const int UndefinedErrorCode = 0;

        public DataSchemaError() : this(string.Empty, ErrorSeverity.Unknown)
        {
        }

        public DataSchemaError(string message, ErrorSeverity severity)
            : this(message, string.Empty, severity)
        {
        }

        public DataSchemaError(string message, Exception innerException, ErrorSeverity severity)
            : this(message, innerException, string.Empty, 0, severity)
        {
        }
        
        public DataSchemaError(string message, string document, ErrorSeverity severity)
            : this(message, document, 0, 0, DefaultPrefix, UndefinedErrorCode, severity)
        {
        }

        public DataSchemaError(string message, string document, int errorCode, ErrorSeverity severity)
            : this(message, document, 0, 0, DefaultPrefix, errorCode, severity)
        {
        }

        public DataSchemaError(string message, string document, int line, int column, ErrorSeverity severity)
             : this(message, document,line, column, DefaultPrefix, UndefinedErrorCode, severity) 
        {
        }

        public DataSchemaError(DataSchemaError source, ErrorSeverity severity)
            : this(source.Message, source.Document, source.Line, source.Column, source.Prefix, source.ErrorCode, severity) 
        {
        }

        public DataSchemaError(
            Exception exception,
            string prefix,
            int errorCode,
            ErrorSeverity severity)
            : this(exception, string.Empty, 0, 0, prefix, errorCode, severity)
        {
        }

        public DataSchemaError(
            string message,
            Exception exception,
            string prefix,
            int errorCode,
            ErrorSeverity severity)
            : this(message, exception, string.Empty, 0, 0, prefix, errorCode, severity)
        {
        }

        public DataSchemaError(
            Exception exception,
            string document,
            int line,
            int column,
            string prefix,
            int errorCode,
            ErrorSeverity severity)
            : this(exception.Message, exception, document, line, column, prefix, errorCode, severity)
        {
        }

        public DataSchemaError(
            string message,
            string document,
            int line,
            int column,
            string prefix,
            int errorCode,
            ErrorSeverity severity)
            : this(message, null, document, line, column, prefix, errorCode, severity)
        {
        }

        public DataSchemaError(
            string message,
            Exception exception,
            string document,
            int line,
            int column,
            string prefix,
            int errorCode,
            ErrorSeverity severity)
        {
            if (errorCode > MaxErrorCode || errorCode < 0)
            {
                throw new ArgumentOutOfRangeException("errorCode");
            }

            Document = document;
            Severity = severity;
            Line = line;
            Column = column;
            Message = message;
            Exception = exception;

            ErrorCode = errorCode;
            Prefix = prefix;
            IsPriorityEditable = true;
        }

        /// <summary>
        /// The filename of the error. It corresponds to the File column on the Visual Studio Error List window.
        /// </summary>
        public string Document { get; set; }

        /// <summary>
        /// The severity of the error
        /// </summary>
        public ErrorSeverity Severity { get; private set; }

        public int ErrorCode { get; private set; }

        /// <summary>
        /// Line Number of the error
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column Number of the error
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Prefix of the error
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// If the error has any special help topic, this property may hold the ID to the same.
        /// </summary>
        public string HelpKeyword { get; set; }

        /// <summary>
        /// Exception associated with the error, or null
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Message 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Should this message honor the "treat warnings as error" flag?
        /// </summary>
        public Boolean IsPriorityEditable { get; set; }

        /// <summary>
        /// Represents the error code used in MSBuild output.  This is the prefix and the
        /// error code
        /// </summary>
        /// <returns></returns>
        public string BuildErrorCode
        {
            get { return FormatErrorCode(Prefix, ErrorCode); }
        }

        public Boolean IsBuildErrorCodeDefined
        {
            get { return (ErrorCode != UndefinedErrorCode); }
        }

        /// <summary>
        /// true if this error is being displayed in ErrorList. More of an Accounting Mechanism to be used internally.
        /// </summary>
        public bool IsOnDisplay { get; set; }

        public static string FormatErrorCode(string prefix, int code)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1:d5}",
                prefix,
                code);
        }

        /// <summary>
        /// String form of this error.
        /// NB: This is for debugging only.
        /// </summary>
        /// <returns>String form of the error.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} - {1}({2},{3}): {4}", FormatErrorCode(Prefix, ErrorCode), Document, Line, Column, Message);
        }
    }
}
