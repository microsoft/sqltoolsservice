//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Microsoft.SqlTools.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.AsyncRequest
{
    /// <summary>
    /// Handler to run a request async
    /// </summary>
    /// <typeparam name="TResponse">Request result</typeparam>
    public class AsyncRequestHandler<TResponse> where TResponse : AsyncSqlResponse, new()
    {
        /// <summary>
        /// Create new instance
        /// </summary>
        /// <param name="requestContext">Request Context</param>
        /// <param name="funcToRun">The function to run the reqest and create the result</param>
        /// <param name="requestParams">Request parameters</param>
        public AsyncRequestHandler(
            RequestContext<TResponse> requestContext, 
            Func<AsyncRequestParams, TResponse> funcToRun, 
            AsyncRequestParams requestParams
            )
        {
            this.TaskRun = funcToRun;
            this.requestContext = requestContext;
            this.requestParams = requestParams;
        }

        private AsyncRequestParams requestParams;
        private Func<AsyncRequestParams, TResponse> TaskRun { get; set; }
        private RequestContext<TResponse> requestContext;

        /// <summary>
        /// Static method to handle a request async
        /// </summary>
        /// <param name="requestContext">Request Context</param>
        /// <param name="funcToRun">The function to run the reqest and create the result</param>
        /// <param name="requestParams">Request parameters</param>
        /// <param name="timeoutInSec">Number of seconds to wait to create response. The request gets canceled if timesout</param>
        public static void HandleRequestAsync<T>(
            RequestContext<T> requestContext, 
            Func<AsyncRequestParams, T> funcToRun, 
            AsyncRequestParams requestParams,
            int timeoutInSec
            ) 
            where T : AsyncSqlResponse, new()
        {
            AsyncRequestHandler<T> asyncSqlRequest = new AsyncRequestHandler<T>(requestContext, funcToRun, requestParams);
            asyncSqlRequest.Run(timeoutInSec);
        }

        private async Task RunFuncAsync(CancellationToken cancellationToken)
        {
            TResponse response = TaskRun(requestParams);
            response.OwnerUri = requestParams.OwnerUri;
            if (cancellationToken.IsCancellationRequested)
            {
                Logger.Write(LogLevel.Verbose, "Sql async request canceled");
            }
            else
            {
                await requestContext.SendResult(response);
            }
        }

        private async Task<AsyncRequestTaskResult> RunTaskWithTimeout(Task task, int timeoutInSec)
        {
            AsyncRequestTaskResult result = new AsyncRequestTaskResult();
            if (timeoutInSec <= 0)
            {
                timeoutInSec = -1;
            }
            TimeSpan timeout = TimeSpan.FromSeconds(timeoutInSec);
            await Task.WhenAny(task, Task.Delay(timeout));
            result.IsCompleted = task.IsCompleted;
            if (task.Exception != null)
            {
                result.Exception = task.Exception;
            }
            else if (!task.IsCompleted)
            {
                result.Exception = new TimeoutException($"Task didn't complete within {timeoutInSec} seconds.");
            }
            return result;
        }

        private void Run(int timeoutInSec)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Task task = Task.Run(async () => await RunFuncAsync(cancellationTokenSource.Token));
            Task.Run(async () =>
            {
                AsyncRequestTaskResult taskResult = await RunTaskWithTimeout(task, timeoutInSec);

                if (taskResult != null && !taskResult.IsCompleted)
                {
                    cancellationTokenSource.Cancel();
                    TResponse response = new TResponse();
                    response.OwnerUri = requestParams.OwnerUri;
                    response.ErrorMessage = taskResult.Exception != null ? taskResult.Exception.Message : $"Failed to complete a sql async request";
                    await requestContext.SendResult(response);
                }
                return taskResult;
            }).ContinueWithOnFaulted(null);
        }
    }

    internal class AsyncRequestTaskResult
    {
        public bool IsCompleted { get; set; }
        public Exception Exception { get; set; }
    }
}
