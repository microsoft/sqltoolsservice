// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
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
        /// <param name="antecedent">The task to continue</param>
        /// <param name="continuationAction">
        /// An optional operation to perform after exception handling has occurred
        /// </param>
        /// <returns>Task with exception handling on continuation</returns>
        public static Task ContinueWithOnFaulted(this Task antecedent, Action<Task> continuationAction)
        {
            return antecedent.ContinueWith(task =>
            {
                // If the task hasn't faulted or has an exception, skip processing
                if (!task.IsFaulted || task.Exception == null)
                {
                    return;
                }
                
                LogTaskExceptions(task.Exception);

                // Run the continuation task that was provided
                try
                {
                    continuationAction?.Invoke(task);
                }
                catch (Exception e)
                {
                    Logger.Write(TraceEventType.Error, $"Exception in exception handling continuation: {e}");
                    Logger.Write(TraceEventType.Error, e.StackTrace);
                }
            });
        }

        /// <summary>
        /// Adds handling to check the Exception field of a task and log it if the task faulted.
        /// This version allows for async code to be ran in the continuation function.
        /// </summary>
        /// <remarks>
        /// This will effectively swallow exceptions in the task chain. 
        /// </remarks>
        /// <param name="antecedent">The task to continue</param>
        /// <param name="continuationFunc">
        /// An optional operation to perform after exception handling has occurred
        /// </param>
        /// <returns>Task with exception handling on continuation</returns>
        public static Task ContinueWithOnFaulted(this Task antecedent, Func<Task, Task> continuationFunc)
        {
            return antecedent.ContinueWith(task =>
            {
                // If the task hasn't faulted or doesn't have an exception, skip processing
                if (!task.IsFaulted || task.Exception == null)
                {
                    return;
                }

                LogTaskExceptions(task.Exception);

                // Run the continuation task that was provided
                try
                {
                    continuationFunc?.Invoke(antecedent).Wait();
                }
                catch (Exception e)
                {
                    Logger.Write(TraceEventType.Error, $"Exception in exception handling continuation: {e}");
                    Logger.Write(TraceEventType.Error, e.StackTrace);
                }
            });
        }

        private static void LogTaskExceptions(AggregateException exception)
        {
            // Construct an error message for an aggregate exception and log it
            StringBuilder sb = new StringBuilder("Unhandled exception(s) in async task:");
            foreach (Exception e in exception.InnerExceptions)
            {
                sb.AppendLine($"{e.GetType().Name}: {e.Message}");
                sb.AppendLine(e.StackTrace);
            }
            Logger.Write(TraceEventType.Error, sb.ToString());
        }
    }
}