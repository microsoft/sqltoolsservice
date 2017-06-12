//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.TaskServices;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.TaskServices
{
    public class DatabaseOperationStub
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public void Stop()
        {
            IsStopped = true;
        }

        public void FailTheOperation()
        {
            Failed = true;
        }

        public TaskResult TaskResult { get; set; }

        public bool IsStopped { get; set; }

        public bool Failed { get; set; }

        public async Task<TaskResult> FunctionToRun(SqlTask sqlTask)
        {
            sqlTask.TaskCanceled += OnTaskCanceled;
            return await Task.Factory.StartNew(() =>
            {
                while (!IsStopped)
                {
                    //Just keep running
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                    if (Failed)
                    {
                        throw new InvalidOperationException();
                    }
                    sqlTask.AddMessage("still running", SqlTaskStatus.InProgress, true);
                }
                sqlTask.AddMessage("done!", SqlTaskStatus.Succeeded);

                return TaskResult;
            });
        }

        private void OnTaskCanceled(object sender, TaskEventArgs<SqlTaskStatus> e)
        {
            cancellationTokenSource.Cancel();
        }
    }
}
