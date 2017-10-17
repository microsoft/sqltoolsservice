// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Adds handling to check the Exception field of a task and log it if the task faulted
        /// </summary>
        /// <remarks>
        /// This will effectively swallow exceptions in the task chain. 
        /// </remarks>
        /// <param name="task">The task to continue</param>
        /// <param name="continuationAction">
        /// An optional operation to perform after exception handling has occurred
        /// </param>
        /// <returns>Task with exception handling on continuation</returns>
        public static Task ContinueWithOnFaulted(this Task task, Action<Task> continuationAction)
        {
            return task.ContinueWith(t =>
            {
                // If the task hasn't faulted or has an exception, skip processing
                if (!t.IsFaulted || t.Exception == null)
                {
                    return;
                }
                
                // Construct an error message for an aggregate exception and log it
                StringBuilder sb = new StringBuilder("Unhandled exception(s) in async task:");
                foreach (Exception e in task.Exception.InnerExceptions)
                {
                    sb.AppendLine($"{e.GetType().Name}: {e.Message}");
                    sb.AppendLine(e.StackTrace);
                }
                Logger.Write(LogLevel.Error, sb.ToString());

                // Run the continuation task that was provided
                continuationAction?.Invoke(t);
            });
        }
    }
}