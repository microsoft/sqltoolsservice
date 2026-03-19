//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.SqlCore.SchemaCompare;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// VSCode/ADS implementation of ISchemaCompareScriptHandler.
    /// Bridges the SqlTask script mechanism to the host-agnostic interface.
    /// </summary>
    internal class VsCodeScriptHandler : ISchemaCompareScriptHandler
    {
        private readonly System.Func<SqlTask> _getTask;

        /// <summary>
        /// Creates a new VsCodeScriptHandler.
        /// The getTask function defers SqlTask retrieval until the task is actually created by SqlTaskManager.
        /// </summary>
        public VsCodeScriptHandler(System.Func<SqlTask> getTask)
        {
            _getTask = getTask;
        }

        public void OnScriptGenerated(string script)
        {
            _getTask()?.AddScript(SqlTaskStatus.Succeeded, script);
        }

        public void OnMasterScriptGenerated(string masterScript)
        {
            _getTask()?.AddScript(SqlTaskStatus.Succeeded, masterScript);
        }
    }
}
