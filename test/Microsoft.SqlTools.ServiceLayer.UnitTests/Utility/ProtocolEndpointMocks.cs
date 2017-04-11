//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Moq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public static class ProtocolEndpointMocks
    {
        public static Mock<IProtocolEndpoint> AddEventHandling<TParams>(
            this Mock<IProtocolEndpoint> mock,
            EventType<TParams> expectedEvent,
            Action<EventType<TParams>, TParams> eventCallback)
        {
            var flow = mock.Setup(h => h.SendEvent(
                It.Is<EventType<TParams>>(m => m == expectedEvent),
                It.IsAny<TParams>()))
                .Returns(Task.FromResult(0));
            if (eventCallback != null)
            {
                flow.Callback(eventCallback);
            }

            return mock;
        }
    }
}
