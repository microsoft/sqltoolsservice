//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.RpcTestUtilities
{
    public sealed class RpcResultCapture<TResponse>
    {
        private readonly Action<TResponse> resultCallback;

        public RpcResultCapture(Action<TResponse> resultCallback = null)
        {
            this.resultCallback = resultCallback;
        }

        public TResponse Result { get; private set; }

        public bool HasResult { get; private set; }

        public Task SetResult(TResponse result)
        {
            this.Result = result;
            this.HasResult = true;
            this.resultCallback?.Invoke(result);
            return Task.CompletedTask;
        }
    }

    public static class RpcResultCaptures
    {
        public static RpcResultCapture<TResponse> Create<TResponse>(Action<TResponse> resultCallback)
        {
            return new RpcResultCapture<TResponse>(resultCallback);
        }

        public static RpcResultCapture<TResponse> AddErrorHandling<TResponse>(
            this RpcResultCapture<TResponse> capture,
            Action<string, int, string> errorCallback)
        {
            return capture;
        }
    }

    public class ResultStatusCapture<T> where T : ResultStatus
    {
        private T result;
        public T Result => result ?? throw new InvalidOperationException("No result has been returned");

        public async Task SetResult(T actual)
        {
            result = actual;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Asserts that this result was successful.
        /// </summary>
        /// <param name="handlerName">Name of the handler, recommended to use nameof(service.MyHandler), used in the failure message</param>
        /// <param name="descriptor">Optional extra descriptor, parenthesized in the failure message</param>
        public void AssertSuccess(string handlerName, string descriptor = null)
        {
            Assert.IsTrue(this.Result.Success, $"{handlerName}{(descriptor != null ? $" ({descriptor})" : String.Empty)} expected to succeed, but failed with error: '{this.Result.ErrorMessage}'");
        }
    }
}
