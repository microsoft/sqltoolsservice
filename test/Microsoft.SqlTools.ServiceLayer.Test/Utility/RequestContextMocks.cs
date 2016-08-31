//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol.Contracts;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.Test.Utility
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
            Action<object> errorCallback)
        {

            // Setup the mock for SendError
            var sendErrorFlow = mock.Setup(rc => rc.SendError(It.IsAny<object>()))
                .Returns(Task.FromResult(0));
            if (mock != null)
            {
                sendErrorFlow.Callback(errorCallback);
            }

            return mock;
        }

        public static Mock<RequestContext<TResponse>> SetupRequestContextMock<TResponse, TParams>(
            Action<TResponse> resultCallback,
            EventType<TParams> expectedEvent,
            Action<EventType<TParams>, TParams> eventCallback,
            Action<object> errorCallback)
        {
            return Create(resultCallback)
                .AddEventHandling(expectedEvent, eventCallback)
                .AddErrorHandling(errorCallback);            
        }

    }
}
