//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking
{
    public class EventFlowValidator<TRequestContext>
    {
        private List<ExpectedEvent> ExpectedEvents { get; } = new List<ExpectedEvent>();
        private List<ReceivedEvent> ReceivedUpdateEvents { get; } = new List<ReceivedEvent>();
        private List<ReceivedEvent> ReceivedEvents { get; } = new List<ReceivedEvent>();
        private Mock<RequestContext<TRequestContext>> Context { get; }
        private bool _completed;

        public EventFlowValidator(MockBehavior behavior = MockBehavior.Strict)
        {
            Context = new Mock<RequestContext<TRequestContext>>(behavior);
        }

        public RequestContext<TRequestContext> Object => Context.Object;
        public EventFlowValidator<TRequestContext> SetupCallbackOnMethodSendEvent<TParams>(EventType<TParams> matchingEvent, Action<TParams> callback)
        {
            Context.Setup(rc => rc.SendEvent(matchingEvent, It.IsAny<TParams>()))
                .Callback<EventType<TParams>, TParams>((et, p) => callback(p))
                .Returns(Task.FromResult(0));
            return this;
        }


        public EventFlowValidator<TRequestContext> AddEventValidation<TParams>(EventType<TParams> expectedEvent, Action<TParams> paramValidation, Action<TParams> userCallback = null)
        {
            ExpectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Event,
                ParamType = typeof(TParams),
                Validator = paramValidation
            });

            Context.Setup(rc => rc.SendEvent(expectedEvent, It.IsAny<TParams>()))
                .Callback<EventType<TParams>, TParams>((et, p) =>
                {
                    ReceivedEvents.Add(new ReceivedEvent
                    {
                        EventObject = p,
                        EventType = EventTypes.Event
                    });
                    userCallback?.DynamicInvoke(p);
                })
                .Returns(Task.FromResult(0));

            return this;
        }

        public EventFlowValidator<TRequestContext> AddResultValidation(Action<TRequestContext> resultValidation)
        {
            // Add the expected event
            ExpectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Result,
                ParamType = typeof(TRequestContext),
                Validator = resultValidation
            });

            return this;
        }

        public EventFlowValidator<TRequestContext> AddSimpleErrorValidation(Action<string, int> paramValidation)
        {
            // Put together a validator that ensures a null data

            // Add the expected result
            ExpectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Error,
                ParamType = typeof(Error),
                Validator = (Action<Error>)(e =>
                {
                    Assert.NotNull(e);
                    paramValidation(e.Message, e.Code);
                })
            });

            return this;
        }

        public EventFlowValidator<TRequestContext> AddStandardErrorValidation()
        {
            // Add an error validator that just ensures a non-empty error message and null data obj
            return AddSimpleErrorValidation((msg, code) =>
            {
                Assert.NotEmpty(msg);
            });
        }

        public EventFlowValidator<TRequestContext> Complete()
        {
            // Add general handler for result handling
            Context.Setup(rc => rc.SendResult(It.IsAny<TRequestContext>()))
                .Callback<TRequestContext>(r => ReceivedEvents.Add(new ReceivedEvent
                {
                    EventObject = r,
                    EventType = EventTypes.Result
                }))
                .Returns(Task.FromResult(0));

            // Add general handler for error event
            Context.AddErrorHandling((msg, code) =>
            {
                ReceivedEvents.Add(new ReceivedEvent
                {
                    EventObject = new Error {Message = msg, Code = code},
                    EventType = EventTypes.Error
                });
            });

            _completed = true;
            return this;
        }

        public void Validate()
        {
            // Make sure the handlers have been added
            if (!_completed)
            {
                throw new Exception("EventFlowValidator must be completed before it can be validated.");
            }

            // Iterate over the two lists in sync to see if they are the same
            for (int i = 0; i < Math.Max(ExpectedEvents.Count, ReceivedEvents.Count); i++)
            {
                // Step 0) Make sure both events exist
                if (i >= ExpectedEvents.Count)
                {
                    throw new Exception($"Unexpected event received: [{ReceivedEvents[i].EventType}] {ReceivedEvents[i].EventObject}");
                }
                ExpectedEvent expected = ExpectedEvents[i];

                if (i >= ReceivedEvents.Count)
                {
                    throw new Exception($"Expected additional events: [{ExpectedEvents[i].EventType}] {ExpectedEvents[i].ParamType}");
                }
                ReceivedEvent received = ReceivedEvents[i];

                // Step 1) Make sure the event type matches
                Assert.True(expected.EventType.Equals(received.EventType),
                    string.Format("Expected EventType {0} but got {1}. Received object is {2}", expected.EventType, received.EventType, received.EventObject.ToString()));
                
                // Step 2) Make sure the param type matches
                Assert.True( expected.ParamType == received.EventObject.GetType()
                    , $"expected and received event types differ for event Number: {i+1}. Expected EventType: {expected.ParamType}  & Received EventType: {received.EventObject.GetType()}\r\n"
                    + $"\there is the full list of expected and received events::"
                    + $"\r\n\t\t expected event types:{string.Join("\r\n\t\t", ExpectedEvents.ConvertAll(evt=>evt.ParamType))}"
                    + $"\r\n\t\t received event types:{string.Join("\r\n\t\t", ReceivedEvents.ConvertAll(evt=>evt.EventObject.GetType()))}"
                );
                
                // Step 3) Run the validator on the param object
                Assert.NotNull(received.EventObject);
                expected.Validator?.DynamicInvoke(received.EventObject);
            }

            // Iterate over updates events if any to ensure that they are conforming
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
