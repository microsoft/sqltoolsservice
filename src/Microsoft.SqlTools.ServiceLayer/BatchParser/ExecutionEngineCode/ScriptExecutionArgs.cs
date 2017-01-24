//------------------------------------------------------------------------------
// <copyright file="ScriptExecutionArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class ScriptExecutionArgs : EventArgs
    {
        #region Private fields

        private IDbConnection _connection;
        private IBatchEventsHandler _batchEventHandlers;
        private int _startingLine;
        private Dictionary<string, string> _cmdVariables;

        #endregion

        #region Constructors / Destructor

        // FUTURE CLEANUP: Remove in favor of general signature (IDbConnection) - #920978
        public ScriptExecutionArgs(
            String script, 
            SqlConnection connection, 
            int timeOut, 
            ExecutionEngineConditions conditions, 
            IBatchEventsHandler batchEventHandlers)
            : this (script, (IDbConnection)connection, timeOut, conditions, batchEventHandlers)
        {
            // nothing
        }

        // FUTURE CLEANUP: Remove in favor of general signature (IDbConnection) - #920978
        public ScriptExecutionArgs(
            String script,
            SqlConnection connection,
            int timeOut,
            ExecutionEngineConditions conditions,
            IBatchEventsHandler batchEventHandlers,
            int startingLine,
            IDictionary<string,string> variables)
            : this(script, (IDbConnection) connection, timeOut, conditions, batchEventHandlers, startingLine, variables)
        {
            // nothing
        }

        public ScriptExecutionArgs(
            String script,
            IDbConnection connection,
            int timeOut,
            ExecutionEngineConditions conditions,
            IBatchEventsHandler batchEventHandlers)
            : this(script, connection, timeOut, conditions, batchEventHandlers, 0, null)
        {
            // nothing
        }

        public ScriptExecutionArgs(
                    String script,
                    IDbConnection connection,
                    int timeOut,
                    ExecutionEngineConditions conditions,
                    IBatchEventsHandler batchEventHandlers,
                    int startingLine,
                    IDictionary<string, string> variables)
        {
            Script = script;
            _connection = connection;
            TimeOut = timeOut;
            Conditions = conditions;
            _batchEventHandlers = batchEventHandlers;
            _startingLine = startingLine;

            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    Variables[variable.Key] = variable.Value;
                }
            }
        }

        #endregion

        #region Public properties

        public string Script { get; set; }

        // FUTURE CLEANUP: Remove in favor of general signature (IDbConnection) - #920978
        public SqlConnection Connection
        {
            get { return _connection as SqlConnection; }
            set { _connection = value as SqlConnection; }
        }

        public IDbConnection ReliableConnection
        {
            get { return _connection; }
            set { _connection = value; }
        }

        public int TimeOut { get; set; }

        internal ExecutionEngineConditions Conditions { get; set; }

        internal IBatchEventsHandler BatchEventHandlers
        {
            get { return _batchEventHandlers; }
            set { _batchEventHandlers = value; }
        }

        internal int StartingLine
        {
            get { return _startingLine; }
            set { _startingLine = value; }
        }

        internal Dictionary<string, string> Variables
        {
            get
            {
                if (_cmdVariables == null)
                {
                    _cmdVariables = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                }

                return _cmdVariables;
            }
        }
        #endregion
    }
}
