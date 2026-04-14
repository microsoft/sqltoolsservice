//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.SqlCore.SchemaCompare;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// VSCode/ADS implementation of ISchemaCompareScriptHandler.
    /// Bridges script delivery to SqlTask.
    /// </summary>
    internal class VsCodeScriptHandler : ISchemaCompareScriptHandler
    {
        private readonly Func<SqlTask> _getTask;

        public VsCodeScriptHandler(Func<SqlTask> getTask)
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
