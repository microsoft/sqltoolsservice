//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare
{
    /// <summary>
    /// Adapter that wraps a host-agnostic operation as an ITaskOperation
    /// for consumption by SqlTaskManager.
    /// </summary>
    internal class SchemaCompareTaskAdapter : ITaskOperation
    {
        private readonly Action _execute;
        private readonly Action _cancel;
        private readonly Func<string> _getError;

        public SqlTask SqlTask { get; set; }

        public string ErrorMessage => _getError?.Invoke();

        public SchemaCompareTaskAdapter(Action execute, Action cancel, Func<string> getError)
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
