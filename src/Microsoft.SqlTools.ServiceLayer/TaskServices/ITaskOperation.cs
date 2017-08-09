using System;
using System.Collections.Generic;
using System.Text;

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
    }

    /// <summary>
    /// Defines interface for scriptable task operations
    /// </summary>
    public interface IScriptableTaskOperation: ITaskOperation
    {
        string ScriptContent { get; set; }
    }
}