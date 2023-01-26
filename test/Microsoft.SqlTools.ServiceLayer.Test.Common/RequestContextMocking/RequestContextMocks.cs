//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking
{
    public static class RequestContextMocks
    {
        public static Mock<RequestContext<TResponse>> Create<TResponse>(Action<TResponse> resultCallback)
        {
            var requestContext = new Mock<RequestContext<TResponse>>();

            // Setup the mock for SendResult
            var sendResultFlow = requestContext
                .Setup(rc => rc.SendResult(It.IsAny<TResponse>()))
                .Returns(Task.FromResult(0));
            if (resultCallback != null)
            {
                sendResultFlow.Callback(resultCallback);
            }
            return requestContext;
        }

        public static Mock<RequestContext<TResponse>> AddEventHandling<TResponse, TParams>(
            this Mock<RequestContext<TResponse>> mock,
            EventType<TParams> expectedEvent,
            Action<EventType<TParams>, TParams> eventCallback)
        {
            var flow = mock.Setup(rc => rc.SendEvent(
                It.Is<EventType<TParams>>(m => m == expectedEvent),
                It.IsAny<TParams>()))
                .Returns(Task.FromResult(0));
            if (eventCallback != null)
            {
                flow.Callback(eventCallback);
            }

            return mock;
        }

        public static Mock<RequestContext<TResponse>> AddErrorHandling<TResponse>(
            this Mock<RequestContext<TResponse>> mock,
            Action<string, int, string> errorCallback)
        {
            // Setup the mock for SendError
            var sendErrorFlow = mock.Setup(rc => rc.SendError(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            if (errorCallback != null)
            {
                sendErrorFlow.Callback<string, int, string>(errorCallback);
            }

            return mock;
        }
    }

    public class MockRequest<T> where T : ResultStatus
    {
        private T? result;
        public T Result => result ?? throw new InvalidOperationException("No result has been sent for the request");

        public Mock<RequestContext<T>> Mock;
        public RequestContext<T> Object => Mock.Object;

        public MockRequest()
        {
            Mock = RequestContextMocks.Create<T>(actual => result = actual);
        }

        /// <summary>
        /// Asserts that this request was successful
        /// </summary>
        /// <param name="handlerName">Name of the handler, recommended to use nameof(service.MyHandler), used in the failure message</param>
        /// <param name="descriptor">Optional extra descriptor, parenthesized in the failure message</param>
        public void AssertSuccess(string handlerName, string? descriptor = null)
        {
            Assert.IsTrue(this.Result.Success, $"{handlerName} {(descriptor != null ? $"(descriptor) " : String.Empty)}expected to succeed, but failed with error: '{this.Result.ErrorMessage}'");
        }
    }
}
