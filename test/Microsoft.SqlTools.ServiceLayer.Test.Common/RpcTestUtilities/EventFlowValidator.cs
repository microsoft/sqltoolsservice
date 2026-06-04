//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common.RpcTestUtilities
{
    public class EventFlowValidator<TResult> : IEventSender
    {
        private List<ExpectedEvent> ExpectedEvents { get; } = new List<ExpectedEvent>();
        private List<ReceivedEvent> ReceivedEvents { get; } = new List<ReceivedEvent>();
        private Dictionary<string, Action<object>> EventCallbacks { get; } = new Dictionary<string, Action<object>>();
        private bool _completed;

        public EventFlowValidator()
        {
        }

        public IEventSender Object => this;

        public EventFlowValidator<TResult> SetupCallbackOnMethodSendEvent<TParams>(EventType<TParams> matchingEvent, Action<TParams> callback)
        {
            EventCallbacks[matchingEvent.MethodName] = p => callback((TParams)p);
            return this;
        }


        public EventFlowValidator<TResult> AddEventValidation<TParams>(EventType<TParams> expectedEvent, Action<TParams> paramValidation, Action<TParams> userCallback = null)
        {
            ExpectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Event,
                ParamType = typeof(TParams),
                Validator = paramValidation
            });

            EventCallbacks[expectedEvent.MethodName] = p =>
            {
                ReceivedEvents.Add(new ReceivedEvent
                {
                    EventObject = p,
                    EventType = EventTypes.Event
                });
                userCallback?.DynamicInvoke(p);
            };

            return this;
        }

        public EventFlowValidator<TResult> AddResultValidation(Action<TResult> resultValidation)
        {
            // Add the expected event
            ExpectedEvents.Add(new ExpectedEvent
            {
                EventType = EventTypes.Result,
                ParamType = typeof(TResult),
                Validator = resultValidation
            });

            return this;
        }

        public EventFlowValidator<TResult> AddSimpleErrorValidation(Action<string, int> paramValidation)
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

        public EventFlowValidator<TResult> AddStandardErrorValidation()
        {
            // Add an error validator that just ensures a non-empty error message and null data obj
            return AddSimpleErrorValidation((msg, code) =>
            {
                Assert.That(msg, Is.Not.Null.Or.Empty, $"AddStandardErrorValidation msg for {code}");
            });
        }

        public EventFlowValidator<TResult> Complete()
        {
            _completed = true;
            return this;
        }

        public Task SetResult(TResult result)
        {
            ReceivedEvents.Add(new ReceivedEvent
            {
                EventObject = result,
                EventType = EventTypes.Result
            });
            return Task.CompletedTask;
        }

        public Task SetError(string message, int code = 0, string data = null)
        {
            ReceivedEvents.Add(new ReceivedEvent
            {
                EventObject = new Error { Message = message, Code = code, Data = data },
                EventType = EventTypes.Error
            });
            return Task.CompletedTask;
        }

        public Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            if (EventCallbacks.TryGetValue(eventType.MethodName, out Action<object> callback))
            {
                callback(eventParams);
            }
            else
            {
                ReceivedEvents.Add(new ReceivedEvent
                {
                    EventObject = eventParams,
                    EventType = EventTypes.Event
                });
            }

            return Task.CompletedTask;
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
                Assert.True(expected.ParamType == received.EventObject.GetType()
                    , $"expected and received event types differ for event Number: {i + 1}. Expected EventType: {expected.ParamType}  & Received EventType: {received.EventObject.GetType()}\r\n"
                    + $"\there is the full list of expected and received events::"
                    + $"\r\n\t\t expected event types:{string.Join("\r\n\t\t", ExpectedEvents.ConvertAll(evt => evt.ParamType))}"
                    + $"\r\n\t\t received event types:{string.Join("\r\n\t\t", ReceivedEvents.ConvertAll(evt => evt.EventObject.GetType()))}"
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

        private sealed class ExpectedEvent
        {
            public EventTypes EventType { get; set; }
            public Type ParamType { get; set; }
            public Delegate Validator { get; set; }
        }

        private sealed class ReceivedEvent
        {
            public object EventObject { get; set; }
            public EventTypes EventType { get; set; }
        }
    }
}
