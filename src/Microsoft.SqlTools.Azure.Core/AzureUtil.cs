//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Azure.Core
{
    internal static class AzureUtil
    {
        /// <summary>
        /// Execute an async action for each input in the a list of input in parallel.
        /// If any task fails, adds the exeption message to the response errors
        /// If cancellation token is set to cancel, returns empty response
        /// </summary>        
        /// <param name="session">Resource management session to use to call the resource manager</param>
        /// <param name="inputs">List of inputs</param>
        /// <param name="serverName">server name to filter the result</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="asyncAction">Async action</param>
        /// <returns>ServiceResponse including the list of data and errors</returns>
        public static async Task<ServiceResponse<TResult>> ExecuteGetAzureResourceAsParallel<TInput, TResult>(
            IAzureResourceManagementSession session,
            IEnumerable<TInput> inputs,
            string serverName,
            CancellationToken cancellationToken,
            Func<IAzureResourceManagementSession,
                TInput,
                string,
                CancellationToken,
                CancellationToken,
                Task<ServiceResponse<TResult>>> asyncAction
            )
        {
            List<TResult> mergedResult = new List<TResult>();
            List<Exception> mergedErrors = new List<Exception>();
            try
            {
                if (inputs == null)
                {
                    return new ServiceResponse<TResult>(mergedResult);
                }
                List<TInput> inputList = inputs.ToList();

                ServiceResponse<TResult>[] resultList = new ServiceResponse<TResult>[inputList.Count];
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                var tasks = Enumerable.Range(0, inputList.Count())
                    .Select(async i =>
                    {
                        ServiceResponse<TResult> result = await GetResult(session, inputList[i], serverName, cancellationToken,
                                cancellationTokenSource.Token, asyncAction);
                        //server name is used to filter the result and if the data is already found not, we need to cancel the other tasks
                        if (!string.IsNullOrEmpty(serverName) && result.Found)
                        {
                            cancellationTokenSource.Cancel();
                        }
                        resultList[i] = result;
                        return result;
                    }
                    );

                await Task.WhenAll(tasks);

                if (!cancellationToken.IsCancellationRequested)
                {
                    foreach (ServiceResponse<TResult> resultForEachInput in resultList)
                    {
                        mergedResult.AddRange(resultForEachInput.Data);
                        mergedErrors.AddRange(resultForEachInput.Errors);
                    }
                }
            }
            catch (Exception ex)
            {
                mergedErrors.Add(ex);
                return new ServiceResponse<TResult>(mergedResult, mergedErrors);
            }
            return new ServiceResponse<TResult>(mergedResult, mergedErrors);
        }

        private static async Task<ServiceResponse<TResult>> GetResult<TInput, TResult>(
            IAzureResourceManagementSession session,
            TInput input,
            string serverName,
            CancellationToken cancellationToken,
            CancellationToken internalCancellationToken,
            Func<IAzureResourceManagementSession,
                TInput,
                string,
                CancellationToken,
                CancellationToken,
                Task<ServiceResponse<TResult>>> asyncAction
            )
        {
            if (cancellationToken.IsCancellationRequested || internalCancellationToken.IsCancellationRequested)
            {
                return new ServiceResponse<TResult>();
            }
            try
            {
                return await asyncAction(session, input, serverName, cancellationToken, internalCancellationToken);
            }
            catch (Exception ex)
            {
                return new ServiceResponse<TResult>(ex);
            }
        }
    }
}
