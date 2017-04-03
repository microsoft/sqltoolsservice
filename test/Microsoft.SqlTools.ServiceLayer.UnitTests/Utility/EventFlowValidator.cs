//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    public class EventFlowValidator<TRequestContext>
    {
        private readonly List<ExpectedEvent> expectedEvents = new List<ExpectedEvent>();
        private readonly List<ReceivedEvent> receivedEvents = new List<ReceivedEvent>();
        private readonly Mock<RequestContext<TRequestContext>> requestContext;
        private bool completed;

        public EventFlowValidator()
        {
            requestContext = new Mock<RequestContext<TRequestContext>>(MockBehavior.Strict);
        }

        public RequestContext<TRequestContext> Object
        {
            get { return requestContext.Object; }
        }

        public EventFlowValidator<TRequestContext> AddEventValidation<TParams>(EventType<TParams> expectedEvent, Action<TParams> paramValidation)
        {
            expectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Event,
                ParamType = typeof(TParams),
                Validator = paramValidation
            });

            requestContext.Setup(rc => rc.SendEvent(expectedEvent, It.IsAny<TParams>()))
                .Callback<EventType<TParams>, TParams>((et, p) =>
                {
                    receivedEvents.Add(new ReceivedEvent
                    {
                        EventObject = p,
                        EventType = EventTypes.Event
                    });
                })
                .Returns(Task.FromResult(0));

            return this;
        }

        public EventFlowValidator<TRequestContext> AddResultValidation(Action<TRequestContext> paramValidation)
        {
            // Add the expected event
            expectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Result,
                ParamType = typeof(TRequestContext),
                Validator = paramValidation
            });

            return this;
        }

        public EventFlowValidator<TRequestContext> AddErrorValidation(Action<Error> paramValidation)
        {
            // Add the expected result
            expectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Error,
                ParamType = typeof(Error),
                Validator = paramValidation
            });

            return this;
        }

        public EventFlowValidator<TRequestContext> AddStandardErrorValidation()
        {
            // Add an error validator that just ensures a non-empty error message
            return AddErrorValidation(e => Assert.NotEmpty(e.Message));
        }

        public EventFlowValidator<TRequestContext> Complete()
        {
            // Add general handler for result handling
            requestContext.Setup(rc => rc.SendResult(It.IsAny<TRequestContext>()))
                .Callback<TRequestContext>(r => receivedEvents.Add(new ReceivedEvent
                {
                    EventObject = r,
                    EventType = EventTypes.Result
                }))
                .Returns(Task.FromResult(0));

            // Add general handler for error event
            requestContext.AddErrorHandling((msg, code) =>
            {
                receivedEvents.Add(new ReceivedEvent
                {
                    EventObject = new Error {Message = msg, Code = code},
                    EventType = EventTypes.Error
                });
            });

            completed = true;
            return this;
        }

        public void Validate()
        {
            // Make sure the handlers have been added
            if (!completed)
            {
                throw new Exception("EventFlowValidator must be completed before it can be validated.");
            }

            // Iterate over the two lists in sync to see if they are the same
            for (int i = 0; i < Math.Max(expectedEvents.Count, receivedEvents.Count); i++)
            {
                // Step 0) Make sure both events exist
                if (i >= expectedEvents.Count)
                {
                    throw new Exception($"Unexpected event received: [{receivedEvents[i].EventType}] {receivedEvents[i].EventObject}");
                }
                ExpectedEvent expected = expectedEvents[i];

                if (i >= receivedEvents.Count)
                {
                    throw new Exception($"Expected additional events: [{expectedEvents[i].EventType}] {expectedEvents[i].ParamType}");
                }
                ReceivedEvent received = receivedEvents[i];

                // Step 1) Make sure the event type matches
                Assert.Equal(expected.EventType, received.EventType);
                
                // Step 2) Make sure the param type matches
                Assert.Equal(expected.ParamType, received.EventObject.GetType());
                
                // Step 3) Run the validator on the param object
                Assert.NotNull(received.EventObject);
                expected.Validator?.DynamicInvoke(received.EventObject);
            }
        }

        private enum EventTypes
        {
            Result,
            Error,
            Event
        }

        private class ExpectedEvent
        {
            public EventTypes EventType { get; set; }
            public Type ParamType { get; set; }
            public Delegate Validator { get; set; }
        }

        private class ReceivedEvent
        {
            public object EventObject { get; set; }
            public EventTypes EventType { get; set; }
        }
    }
}
