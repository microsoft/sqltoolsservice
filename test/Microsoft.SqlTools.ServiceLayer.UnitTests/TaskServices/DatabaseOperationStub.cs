﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

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

        public TaskScript TaskScript { get; set; }

        public TaskResult TaskResult { get; set; }

        public bool IsStopped { get; set; }

        public bool Failed { get; set; }

        public async Task<TaskResult> FunctionToRun(SqlTask sqlTask)
        {
            return await Task.Factory.StartNew(() =>
            {
                while (!IsStopped)
                {
                    //Just keep running
                    if (sqlTask.TaskStatus == SqlTaskStatus.Canceled)
                    {
                        break;
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

        public async Task<TaskResult> FunctionToCancel(SqlTask sqlTask)
        {
            return await Task.Factory.StartNew(() =>
            {
                return new TaskResult
                {
                    TaskStatus = SqlTaskStatus.Canceled
                };
            });
        }

        public async Task<TaskResult> FunctionToScript(SqlTask sqlTask)
        {
            return await Task.Factory.StartNew(() =>
            {
                sqlTask.AddMessage("start scripting", SqlTaskStatus.InProgress, true);
                TaskScript = sqlTask.AddScript(SqlTaskStatus.Succeeded, "script generated!");
                sqlTask.AddMessage("done", SqlTaskStatus.Succeeded);

                return new TaskResult
                {
                    TaskStatus = SqlTaskStatus.Succeeded,
                    
                };
            });
        }
    }
}
