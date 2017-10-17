
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public static class TaskExtensions
    {
        public static Task ContinueWithExceptionHandling(this Task task, Action<Task> continuationAction)
        {
            Task intermediateTask = task.ContinueWith(
                t =>
                {
                    // Construct an error message for an aggregate exception
                    StringBuilder sb = new StringBuilder("Unhandled exception(s) in async task:");
                    foreach (Exception e in task.Exception.InnerExceptions)
                    {
                        sb.AppendLine($"{e.GetType().Name}: {e.Message}");
                        sb.AppendLine(e.StackTrace);
                    }
                    
                    Logger.Write(LogLevel.Error, sb.ToString());
                    
                }, 
                TaskContinuationOptions.OnlyOnFaulted
            );
            return intermediateTask.ContinueWith(continuationAction, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}