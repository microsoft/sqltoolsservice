//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    public class ScriptExecutionArgs : EventArgs
    {
        private IDbConnection connection;
        private IBatchEventsHandler batchEventHandlers;
        private int startingLine;
        private Dictionary<string, string> cmdVariables;

        #region Constructors / Destructor

        /// <summary>
        /// Constructor method for ScriptExecutionArgs
        /// </summary>
        public ScriptExecutionArgs(
            string script, 
            SqlConnection connection, 
            int timeOut, 
            ExecutionEngineConditions conditions, 
            IBatchEventsHandler batchEventHandlers)
            : this (script, (IDbConnection)connection, timeOut, conditions, batchEventHandlers)
        {
            // nothing
        }

        /// <summary>
        /// Constructor method for ScriptExecutionArgs
        /// </summary>
        public ScriptExecutionArgs(
            string script,
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

        /// <summary>
        /// Constructor method for ScriptExecutionArgs
        /// </summary>
        public ScriptExecutionArgs(
            string script,
            IDbConnection connection,
            int timeOut,
            ExecutionEngineConditions conditions,
            IBatchEventsHandler batchEventHandlers)
            : this(script, connection, timeOut, conditions, batchEventHandlers, 0, null)
        {
            // nothing
        }

        /// <summary>
        /// Constructor method for ScriptExecutionArgs
        /// </summary>
        public ScriptExecutionArgs(
                    string script,
                    IDbConnection connection,
                    int timeOut,
                    ExecutionEngineConditions conditions,
                    IBatchEventsHandler batchEventHandlers,
                    int startingLine,
                    IDictionary<string, string> variables)
        {
            Script = script;
            this.connection = connection;
            TimeOut = timeOut;
            Conditions = conditions;
            this.batchEventHandlers = batchEventHandlers;
            this.startingLine = startingLine;

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
            get { return connection as SqlConnection; }
            set { connection = value as SqlConnection; }
        }

        public IDbConnection ReliableConnection
        {
            get { return connection; }
            set { connection = value; }
        }

        public int TimeOut { get; set; }

        internal ExecutionEngineConditions Conditions { get; set; }

        internal IBatchEventsHandler BatchEventHandlers
        {
            get { return batchEventHandlers; }
            set { batchEventHandlers = value; }
        }

        internal int StartingLine
        {
            get { return startingLine; }
            set { startingLine = value; }
        }

        internal Dictionary<string, string> Variables
        {
            get
            {
                if (cmdVariables == null)
                {
                    cmdVariables = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                }

                return cmdVariables;
            }
        }
        #endregion
    }
}
