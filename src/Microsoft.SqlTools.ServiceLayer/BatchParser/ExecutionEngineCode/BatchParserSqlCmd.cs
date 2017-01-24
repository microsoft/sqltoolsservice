//------------------------------------------------------------------------------
// <copyright file="BatchParserSqlCmd.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using System.IO;
using Microsoft.SqlTools.ServiceLayer;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class BatchParserSqlCmd : BatchParser
    {
        #region Public delegates
        public delegate void ConnectionChangedDelegate(SqlConnectionStringBuilder connectionStringBuilder);
        public delegate void ErrorActionChangedDelegate(OnErrorAction ea);        
        #endregion

        #region Constructors / Destructor

        /// <summary>
        /// Constructor taking a Parser instance
        /// </summary>
        /// <param name="parser"></param>
        public BatchParserSqlCmd()
            : base()
        {
            // nothing
        }
        #endregion

        #region Public properties
        internal ConnectionChangedDelegate ConnectionChanged
        {
            get { return _connectionChangedDelegate; }
            set { _connectionChangedDelegate = value; }
        }

        internal ErrorActionChangedDelegate ErrorActionChanged
        {
            get { return _errorActionChangedDelegate; }
            set { _errorActionChangedDelegate = value; }
        }
        #endregion

        #region IVariableResolver

        /// <summary>
        /// Looks for any environment variable or internal variable.
        /// </summary>
        public override string GetVariable(PositionStruct pos, string name)
        {
            if (_variableSubstitutionDisabled)
            {
                return null;
            }

            string value;

            // Internally defined variables have higher precedence over environment variables.
            if (!_internalVariables.TryGetValue(name, out value))
            {
                value = Environment.GetEnvironmentVariables()[name] as string;
            }
            if (value == null)
            {
                RaiseScriptError(String.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionError_VariableNotFound, name), ScriptMessageType.FatalError);
                RaiseHaltParser();
                // TODO: Halt the parser, should get/set variable have ParserAction.Abort/Continue (like original?)
            }

            return value;
        }

        public override void SetVariable(PositionStruct pos, string name, string value)
        {
            if (_variableSubstitutionDisabled)
            {
                return;
            }

            if (value == null)
            {
                if (_internalVariables.ContainsKey(name))
                {
                    _internalVariables.Remove(name);
                }
            }
            else
            {
                _internalVariables[name] = value;
            }
        }

        public Dictionary<String, String> InternalVariables
        {
            get { return _internalVariables; }
            set { _internalVariables = value; }
        }

        #endregion

        #region ICommandHandler Members
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "ppIBatchSource")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "fileName")]
        public override BatchParserAction Include(TextBlock filename, out TextReader stream, out string newFilename)
        {
            stream = null;
            newFilename = null;

            RaiseScriptError(String.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionError_CommandNotSupported, "Include"), ScriptMessageType.Error);
            return BatchParserAction.Abort;
        }

        public override BatchParserAction OnError(Token token, OnErrorAction ea)
        {
            if (_errorActionChangedDelegate != null)
            {
                _errorActionChangedDelegate(ea);
            }
            return BatchParserAction.Continue;
        }

        #endregion

        #region Private fields
        /// <summary>
        /// The internal variables that can be used in SqlCommand substitution.
        /// These variables take precedence over environment variables.
        /// </summary>
        private Dictionary<String, String> _internalVariables = new Dictionary<String, String>(StringComparer.CurrentCultureIgnoreCase);
        private ConnectionChangedDelegate _connectionChangedDelegate;
        private ErrorActionChangedDelegate _errorActionChangedDelegate;
        #endregion
    }
}
