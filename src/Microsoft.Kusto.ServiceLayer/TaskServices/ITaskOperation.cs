//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TaskServices
{
    /// <summary>
    /// Defines interface for task operations
    /// </summary>
    public interface ITaskOperation
    {
        /// <summary>
        /// Execute a task
        /// </summary>
        /// <param name="mode">Task execution mode (e.g. script or execute)</param>
        void Execute(TaskExecutionMode mode);

        /// <summary>
        /// Cancel a task
        /// </summary>
        void Cancel();

        /// <summary>
        /// If an error occurred during task execution, this field contains the error message text
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// The sql task that's executing the operation
        /// </summary>
        SqlTask SqlTask { get; set; }
    }

    /// <summary>
    /// Defines interface for scriptable task operations
    /// </summary>
    public interface IScriptableTaskOperation: ITaskOperation
    {
        /// <summary>
        /// Script for the task operation
        /// </summary>
        string ScriptContent { get; set; }
    }
}