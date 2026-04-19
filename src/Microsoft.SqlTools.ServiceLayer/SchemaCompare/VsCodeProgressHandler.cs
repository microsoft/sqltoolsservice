//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.SqlCore.SchemaCompare;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// VSCode/ADS implementation of ISchemaCompareProgressHandler.
    /// Forwards DacServices progress messages to SqlTask.AddMessage(),
    /// which flows through TaskService → TaskStatusChangedNotification → client UI.
    /// </summary>
    internal class VsCodeProgressHandler : ISchemaCompareProgressHandler
    {
        private readonly Func<SqlTask> _getTask;

        public VsCodeProgressHandler(Func<SqlTask> getTask)
        {
            _getTask = getTask ?? throw new ArgumentNullException(nameof(getTask));
        }

        public void OnProgress(string message, bool isError = false)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            SqlTask task = _getTask.Invoke();
            if (task != null)
            {
                SqlTaskStatus status = isError ? SqlTaskStatus.Failed : SqlTaskStatus.InProgress;
                task.AddMessage(message, status);
            }
        }
    }
}
