﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    /// <summary>
    /// Any SMO operation that supports scripting should implement this class. 
    /// It provides all of the configuration needed to choose between scripting or execution mode, 
    /// hook into the Task manager framework, and send success / completion notifications to the caller.
    /// </summary>
    public abstract class SmoScriptableTaskOperation : IScriptableTaskOperation
    {
        /// <summary>
        /// Script content
        /// </summary>
        public string ScriptContent
        {
            get; set;
        }

        /// <summary>
        /// If an error occurred during task execution, this field contains the error message text
        /// </summary>
        public abstract string ErrorMessage { get; }

        /// <summary>
        /// SMO Server instance used for the operation
        /// </summary>
        public abstract Server Server { get; }

        /// <summary>
        /// Cancels the operation
        /// </summary>
        public abstract void Cancel();

        /// <summary>
        /// Updates messages in sql task given new progress message
        /// </summary>
        /// <param name="message"></param>
        public void OnMessageAdded(TaskMessage message)
        {
            if (this.SqlTask != null)
            {
                this.SqlTask.AddMessage(message.Description, message.Status);
            }
        }

        /// <summary>
        /// Updates scripts in sql task given new script
        /// </summary>
        /// <param name="script"></param>
        public void OnScriptAdded(TaskScript script)
        {
            this.SqlTask.AddScript(script.Status, script.Script, script.ErrorMessage);
        }

        /// <summary>
        /// Executes the operations
        /// </summary>
        public abstract void Execute();


        /// <summary>
        /// Execute the operation for given execution mode
        /// </summary>
        /// <param name="mode"></param>
        public virtual void Execute(TaskExecutionMode mode)
        {
            var currentExecutionMode = Server.ConnectionContext.SqlExecutionModes;
            try
            {
                if (Server != null)
                {
                    Server.ConnectionContext.CapturedSql.Clear();
                    SetExecutionMode(mode);
                }

                Execute();
                GenerateScript(mode);
                
            }
            catch
            {
                throw;
            }
            finally
            {
                Server.ConnectionContext.CapturedSql.Clear();
                Server.ConnectionContext.SqlExecutionModes = currentExecutionMode;
            }

        }

        protected void GenerateScript(TaskExecutionMode mode)
        {
            if (mode == TaskExecutionMode.Script || mode == TaskExecutionMode.ExecuteAndScript)
            {
                this.ScriptContent = GetScriptContent();
                if (SqlTask != null)
                {
                    OnScriptAdded(new TaskScript
                    {
                        Status = SqlTaskStatus.Succeeded,
                        Script = this.ScriptContent
                    });
                }
            }
        }

        protected void SetExecutionMode(TaskExecutionMode mode)
        {
            switch (mode)
            {
                case TaskExecutionMode.Execute:
                    {
                        Server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                        break;
                    }
                case TaskExecutionMode.ExecuteAndScript:
                    {
                        Server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;
                        break;
                    }
                case TaskExecutionMode.Script:
                    {
                        Server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                        break;
                    }
            }
        }

        private string GetScriptContent()
        {
            StringBuilder sb = new StringBuilder();
            foreach (String s in this.Server.ConnectionContext.CapturedSql.Text)
            {
                sb.Append(s);
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// The sql task to run the operations
        /// </summary>
        public SqlTask SqlTask { get; set; }

    }
}
