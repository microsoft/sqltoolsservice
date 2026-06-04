//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

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
        public static async Task<ResultStatus> RunWithErrorHandling(Action action)
        {
            return await RunWithErrorHandling(async () => await Task.Run(action));
        }

        /// <summary>
        /// Asynchronous action with standard ResultStatus
        /// </summary>
        /// <param name="action"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task<ResultStatus> RunWithErrorHandling(Func<Task> action)
        {
            return await RunWithErrorHandling<ResultStatus>(async () =>
            {
                await action();

                return new ResultStatus()
                {
                    Success = true,
                    ErrorMessage = null
                };
            });
        }

        /// <summary>
        /// Synchronous action with custom result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task<T> RunWithErrorHandling<T>(Func<T> action) where T : ResultStatus, new()
        {
            return await RunWithErrorHandling<T>(async () => await Task.Run(action));
        }

        /// <summary>
        /// Asynchronous action with custom result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public static async Task<T> RunWithErrorHandling<T>(Func<Task<T>> action) where T : ResultStatus, new()
        {
            try
            {
                T result = await action();
                return result;
            }
            catch (Exception ex)
            {
                return new T()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion
    }
}
