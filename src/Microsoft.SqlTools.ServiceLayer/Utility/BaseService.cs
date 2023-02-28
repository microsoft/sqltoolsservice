//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Base class for services that contains helpful methods
    /// </summary>
    public abstract class BaseService
    {
        #region Runners with error handling

        /// <summary>
        /// Synchronous action with standard ResultStatus
        /// </summary>
        /// <param name="action"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task RunWithErrorHandling(Action action, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling(async () => await Task.Run(action), requestContext);
        }

        /// <summary>
        /// Asynchronous action with standard ResultStatus
        /// </summary>
        /// <param name="action"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task RunWithErrorHandling(Func<Task> action, RequestContext<ResultStatus> requestContext)
        {
            await RunWithErrorHandling<ResultStatus>(async () =>
            {
                await action();

                return new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = null
                };
            }, requestContext);
        }

        /// <summary>
        /// Synchronous action with custom result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task RunWithErrorHandling<T>(Func<T> action, RequestContext<T> requestContext) where T : ResultStatus, new()
        {
            await RunWithErrorHandling<T>(async () => await Task.Run(action), requestContext);
        }

        /// <summary>
        /// Asynchronous action with custom result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task RunWithErrorHandling<T>(Func<Task<T>> action, RequestContext<T> requestContext) where T : ResultStatus, new()
        {
            try
            {
                T result = await action();
                await requestContext.SendResult(result);
            }
            catch (Exception ex)
            {
                await requestContext.SendResult(new T()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        #endregion
    }
}
