//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Adapts a host-agnostic SqlCore operation to the ITaskOperation interface
    /// required by SqlTaskManager. Allows SqlCore operations to be run as SqlTasks
    /// for VSCode/ADS without the operations themselves depending on ServiceLayer types.
    /// </summary>
    internal class SchemaCompareTaskAdapter : ITaskOperation
    {
        private readonly System.Action _execute;
        private readonly System.Action _cancel;
        private readonly System.Func<string> _getError;

        public SqlTask SqlTask { get; set; }

        public string ErrorMessage => _getError?.Invoke();

        /// <summary>
        /// Creates a task adapter wrapping the given execute/cancel/error delegates.
        /// </summary>
        public SchemaCompareTaskAdapter(System.Action execute, System.Action cancel, System.Func<string> getError)
        {
            _execute = execute;
            _cancel = cancel;
            _getError = getError;
        }

        public void Execute(TaskExecutionMode mode)
        {
            _execute();
        }

        public void Cancel()
        {
            _cancel();
        }
    }
}
