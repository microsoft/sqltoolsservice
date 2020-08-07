//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Channels;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.ProtocolTests
{
    [TestFixture]
    public class JsonRpcHostTests
    {
        [Test]
        public void ConstructWithNullProtocolChannel()
        {
            // If: I construct a JSON RPC host with a null protocol channel
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => new JsonRpcHost(null));
        }

        #region SetRequestHandler Tests
        
        [Test]
        public void SetAsyncRequestHandlerNullRequestType()
        {
            // If: I assign a request handler on the JSON RPC host with a null request type
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.Throws<ArgumentNullException>(() =>
                jh.SetAsyncRequestHandler<object, object>(null, (a, b) => Task.FromResult(false)));
        }

        [Test]
        public void SetAsyncRequestHandlerNullRequestHandler()
        {
            // If: I assign a request handler on the JSON RPC host with a null request handler
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object); 
            Assert.Throws<ArgumentNullException>(() => jh.SetAsyncRequestHandler(CommonObjects.RequestType, null));
        }
        
        [Test]
        public void SetSyncRequestHandlerNullRequestHandler()
        {
            // If: I assign a request handler on the JSON RPC host with a null request handler
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object); 
            Assert.Throws<ArgumentNullException>(() => jh.SetRequestHandler(CommonObjects.RequestType, null));
        }
        
        [Test]
        public async Task SetAsyncRequestHandler([Values]bool nullContents)
        {           
            // Setup: Create a mock request handler
            var requestHandler = new Mock<Func<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>, Task>>();
            var message = nullContents
                ? Message.CreateRequest(CommonObjects.RequestType, CommonObjects.MessageId, null)
                : CommonObjects.RequestMessage;
            
            // If: I assign a request handler on the JSON RPC host
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            jh.SetAsyncRequestHandler(CommonObjects.RequestType, requestHandler.Object);

            // Then: It should be the only request handler set
            Assert.That(jh.requestHandlers.Keys, Is.EqualTo(new[] { CommonObjects.RequestType.MethodName }), "requestHandlers.Keys after SetAsyncRequestHandler");
            
            // If: I call the stored request handler
            await jh.requestHandlers[CommonObjects.RequestType.MethodName](message);
            await jh.requestHandlers[CommonObjects.RequestType.MethodName](message);
            
            // Then: The request handler should have been called with the params and a proper request context
            var expectedContents = nullContents
                ? null
                : CommonObjects.TestMessageContents.DefaultInstance;
            requestHandler.Verify(a => a(
                It.Is<CommonObjects.TestMessageContents>(p => p == expectedContents),
                It.Is<RequestContext<CommonObjects.TestMessageContents>>(rc => rc.messageQueue == jh.outputQueue && rc.requestMessage == message)
            ), Times.Exactly(2));
        }

        [Test]
        public async Task SetSyncRequestHandler([Values]bool nullContents)
        {
            // Setup: Create a mock request handler
            var requestHandler = new Mock<Action<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>>>();
            var message = nullContents
                ? Message.CreateRequest(CommonObjects.RequestType, CommonObjects.MessageId, null)
                : CommonObjects.RequestMessage;
            
            // If: I assign a request handler on the JSON RPC host
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            jh.SetRequestHandler(CommonObjects.RequestType, requestHandler.Object);

            // Then: It should be the only request handler set
            Assert.That(jh.requestHandlers.Keys, Is.EqualTo(new[] { CommonObjects.RequestType.MethodName }), "requestHandlers.Keys after SetRequestHandler");

            // If: I call the stored request handler
            await jh.requestHandlers[CommonObjects.RequestType.MethodName](message);
            await jh.requestHandlers[CommonObjects.RequestType.MethodName](message);

            // Then: The request handler should have been called with the params and a proper request context
            var expectedContents = nullContents
                ? null
                : CommonObjects.TestMessageContents.DefaultInstance;
            requestHandler.Verify(a => a(
                It.Is<CommonObjects.TestMessageContents>(p => p == expectedContents),
                It.Is<RequestContext<CommonObjects.TestMessageContents>>(rc => rc.messageQueue == jh.outputQueue && rc.requestMessage == message)
            ), Times.Exactly(2));
        }

        [Test]
        public async Task SetAsyncRequestHandlerOverrideTrue()
        {
            // Setup: Create two mock request handlers
            var requestHandler1 = new Mock<Func<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>, Task>>();
            var requestHandler2 = new Mock<Func<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>, Task>>();
            
            // If: 
            // ... I assign a request handler on the JSON RPC host
            // ... And I reassign the request handler with an override
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            jh.SetAsyncRequestHandler(CommonObjects.RequestType, requestHandler1.Object);
            jh.SetAsyncRequestHandler(CommonObjects.RequestType, requestHandler2.Object, true);

            // Then: There should only be one request handler
            Assert.That(jh.requestHandlers.Keys, Is.EqualTo(new[] { CommonObjects.RequestType.MethodName }), "requestHandlers.Keys after SetAsyncRequestHandler with override");

            // If: I call the stored request handler
            await jh.requestHandlers[CommonObjects.RequestType.MethodName](CommonObjects.RequestMessage);
            
            // Then: The correct request handler should have been called
            requestHandler2.Verify(a => a(
                It.Is<CommonObjects.TestMessageContents>(p => p.Equals(CommonObjects.TestMessageContents.DefaultInstance)),
                It.Is<RequestContext<CommonObjects.TestMessageContents>>(p => p.messageQueue == jh.outputQueue && p.requestMessage == CommonObjects.RequestMessage)
            ), Times.Once);
            requestHandler1.Verify(a => a(
                It.IsAny<CommonObjects.TestMessageContents>(),
                It.IsAny<RequestContext<CommonObjects.TestMessageContents>>()
            ), Times.Never);
        }

        [Test]
        public void SetAsyncRequestHandlerOverrideFalse()
        {
            // Setup: Create a mock request handler
            var requestHandler = new Mock<Func<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>, Task>>();
            
            // If:
            // ... I assign a request handler on the JSON RPC host
            // ... And I reassign the request handler without overriding
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            jh.SetAsyncRequestHandler(CommonObjects.RequestType, requestHandler.Object);
            Assert.That(() => jh.SetAsyncRequestHandler(CommonObjects.RequestType, requestHandler.Object), Throws.InstanceOf<Exception>(), "Reassign request handler without overriding");
        }
        
        #endregion

        #region SetEventHandler Tests

        [Test]
        public void SetAsyncEventHandlerNullEventType()
        {
            // If: I assign an event handler on the JSON RPC host with a null event type
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.Throws<ArgumentNullException>(() =>
                jh.SetAsyncEventHandler<object>(null, (a, b) => Task.FromResult(false)));
        }

        [Test]
        public void SetAsyncEventHandlerNull()
        {
            // If: I assign an event handler on the message gispatcher with a null event handler
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.Throws<ArgumentNullException>(() => jh.SetAsyncEventHandler(CommonObjects.EventType, null));
        }
        
        [Test]
        public void SetSyncEventHandlerNull()
        {
            // If: I assign an event handler on the message gispatcher with a null event handler
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.Throws<ArgumentNullException>(() => jh.SetEventHandler(CommonObjects.EventType, null));
        }

        [Test]
        public async Task SetAsyncEventHandler([Values]bool nullContents)
        {
            // Setup: Create a mock request handler
            var eventHandler = new Mock<Func<CommonObjects.TestMessageContents, EventContext, Task>>();
            var message = nullContents
                ? Message.CreateEvent(CommonObjects.EventType, null)
                : CommonObjects.EventMessage;
            
            // If: I assign an event handler on the JSON RPC host
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            jh.SetAsyncEventHandler(CommonObjects.EventType, eventHandler.Object);

            // Then: It should be the only event handler set
            Assert.That(jh.eventHandlers.Keys, Is.EqualTo(new[] { CommonObjects.EventType.MethodName }), "eventHandlers.Keys after SetAsyncRequestHandler");

            // If: I call the stored event handler
            await jh.eventHandlers[CommonObjects.EventType.MethodName](message);
            await jh.eventHandlers[CommonObjects.EventType.MethodName](message);
            
            // Then: The event handler should have been called with the params and a proper event context
            var expectedContents = nullContents
                ? null
                : CommonObjects.TestMessageContents.DefaultInstance;
            eventHandler.Verify(a => a(
                It.Is<CommonObjects.TestMessageContents>(p => p == expectedContents),
                It.Is<EventContext>(p => p.messageQueue == jh.outputQueue)
            ), Times.Exactly(2));
        }
        
        [Test]
        public async Task SetSyncEventHandler([Values]bool nullContents)
        {
            // Setup: Create a mock request handler
            var eventHandler = new Mock<Action<CommonObjects.TestMessageContents, EventContext>>();
            var message = nullContents
                ? Message.CreateEvent(CommonObjects.EventType, null)
                : CommonObjects.EventMessage;
            
            // If: I assign an event handler on the JSON RPC host
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            jh.SetEventHandler(CommonObjects.EventType, eventHandler.Object);
            
            // Then: It should be the only event handler set
            Assert.That(jh.eventHandlers.Keys, Is.EqualTo(new object[] { CommonObjects.EventType.MethodName }), "eventHandlers.Keys after SetEventHandler");
            
            // If: I call the stored event handler
            await jh.eventHandlers[CommonObjects.EventType.MethodName](message);
            await jh.eventHandlers[CommonObjects.EventType.MethodName](message);
            
            // Then: The event handler should have been called with the params and a proper event context
            var expectedContents = nullContents
                ? null
                : CommonObjects.TestMessageContents.DefaultInstance;
            eventHandler.Verify(a => a(
                It.Is<CommonObjects.TestMessageContents>(p => p == expectedContents),
                It.Is<EventContext>(p => p.messageQueue == jh.outputQueue)
            ), Times.Exactly(2));
        }

        [Test]
        public async Task SetAsyncEventHandlerOverrideTrue()
        {
            // Setup: Create two mock event handlers
            var eventHandler1 = new Mock<Func<CommonObjects.TestMessageContents, EventContext, Task>>();
            var eventHandler2 = new Mock<Func<CommonObjects.TestMessageContents, EventContext, Task>>();
            
            // If:
            // ... I assign an event handler on the JSON RPC host
            // ... And I reassign the event handler with an override
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            
            jh.SetAsyncEventHandler(CommonObjects.EventType, eventHandler1.Object);
            jh.SetAsyncEventHandler(CommonObjects.EventType, eventHandler2.Object, true);
            
            // Then: There should only be one event handler
            Assert.That(jh.eventHandlers.Keys, Is.EqualTo(new[] { CommonObjects.EventType.MethodName }), "requestHandlers.Keys after SetAsyncEventHandler with override");
            
            // If: I call the stored event handler
            await jh.eventHandlers[CommonObjects.EventType.MethodName](CommonObjects.EventMessage);
            
            // Then: The correct event handler should have been called
            eventHandler2.Verify(a => a(
                It.Is<CommonObjects.TestMessageContents>(p => p.Equals(CommonObjects.TestMessageContents.DefaultInstance)),
                It.Is<EventContext>(p => p.messageQueue == jh.outputQueue)
            ), Times.Once);
            eventHandler1.Verify(a => a(
                It.IsAny<CommonObjects.TestMessageContents>(),
                It.IsAny<EventContext>()
            ), Times.Never);
        }

        [Test]
        public void SetAsyncEventHandlerOverrideFalse()
        {
            // Setup: Create a mock event handler
            var eventHandler = new Mock<Func<CommonObjects.TestMessageContents, EventContext, Task>>();
            
            // If:
            // ... I assign an event handler on the JSON RPC host
            // ... And I reassign the event handler without overriding
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            jh.SetAsyncEventHandler(CommonObjects.EventType, eventHandler.Object);
            Assert.That(() => jh.SetAsyncEventHandler(CommonObjects.EventType, eventHandler.Object), Throws.InstanceOf<Exception>(), "SetAsyncEventHandler without overriding");
        }
        
        #endregion
        
        #region SendEvent Tests

        [Test]
        public void SendEventNotConnected()
        {
            // If: I send an event when the protocol channel isn't connected
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.Throws<InvalidOperationException>(() => jh.SendEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance));
        }

        [Test]
        public void SendEvent()
        {
            // Setup: Create a Json RPC Host with a connected channel
            var jh = new JsonRpcHost(GetChannelBase(null, null, true).Object);
            
            // If: I send an event
            jh.SendEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance);

            // Then: The message should be added to the output queue
            Assert.That(jh.outputQueue.ToArray().Select(m => (m.Contents, m.Method)), 
                Is.EqualTo(new[] { (CommonObjects.TestMessageContents.SerializedContents, CommonObjects.EventType.MethodName) }), "outputQueue after SendEvent");
        }
        
        #endregion
        
        #region SendRequest Tests
        
        [Test]
        public async Task SendRequestNotConnected()
        {
            // If: I send an event when the protocol channel isn't connected
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.ThrowsAsync<InvalidOperationException>(() => jh.SendRequest(CommonObjects.RequestType, CommonObjects.TestMessageContents.DefaultInstance));
        }

        [Test]
        public async Task SendRequest()
        {
            // If:  I send a request with the JSON RPC host
            var jh = new JsonRpcHost(GetChannelBase(null, null, true).Object);
            Task<CommonObjects.TestMessageContents> requestTask = jh.SendRequest(CommonObjects.RequestType, CommonObjects.TestMessageContents.DefaultInstance);
            
            // Then: There should be a pending request
            Assert.That(jh.pendingRequests.Count, Is.EqualTo(1), "pendingRequests.Count after SendRequest");
            
            // If: I then trick it into completing the request
            jh.pendingRequests.First().Value.SetResult(CommonObjects.ResponseMessage);
            var responseContents = await requestTask;

            // Then: The returned results should be the contents of the message
            Assert.AreEqual(CommonObjects.TestMessageContents.DefaultInstance, responseContents);
        }
        
        #endregion
        
        #region DispatchMessage Tests

        [Test]
        public async Task DispatchMessageRequestWithoutHandler()
        {
            // Setup: Create a JSON RPC host without a request handler
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            
            // If: I dispatch a request that doesn't have a handler
            // Then: I should get an exception
            Assert.ThrowsAsync<MethodHandlerDoesNotExistException>(() => jh.DispatchMessage(CommonObjects.RequestMessage));
        }

        [Test]
        public async Task DispatchMessageRequestException()
        {
            // Setup: Create a JSON RPC host with a request handler that throws an unhandled exception every time
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            var mockHandler = new Mock<Func<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>, Task>>();
            mockHandler.Setup(f => f(
                    It.IsAny<CommonObjects.TestMessageContents>(),
                    It.IsAny<RequestContext<CommonObjects.TestMessageContents>>()
                ))
                .Returns(Task.FromException(new Exception()));
            jh.SetAsyncRequestHandler(CommonObjects.RequestType, mockHandler.Object);

            // If: I dispatch a message whose handler throws
            // Then: I should get an exception
            Assert.ThrowsAsync<Exception>(() => jh.DispatchMessage(CommonObjects.RequestMessage));
        }
        
        [TestCaseSource(nameof(DispatchMessageWithHandlerData))]
        public async Task DispatchMessageRequestWithHandler(Task result)
        {
            // Setup: Create a JSON RPC host with a request handler setup
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            var mockHandler = new Mock<Func<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>, Task>>();
            mockHandler.Setup(f => f(
                It.Is<CommonObjects.TestMessageContents>(m => m == CommonObjects.TestMessageContents.DefaultInstance),
                It.Is<RequestContext<CommonObjects.TestMessageContents>>(rc => rc.messageQueue == jh.outputQueue)
            )).Returns(result);
            jh.SetAsyncRequestHandler(CommonObjects.RequestType, mockHandler.Object);
            
            // If: I dispatch a request
            await jh.DispatchMessage(CommonObjects.RequestMessage);

            // Then: The request handler should have been called
            mockHandler.Verify(f => f(
                It.Is<CommonObjects.TestMessageContents>(m => m == CommonObjects.TestMessageContents.DefaultInstance),
                It.IsAny<RequestContext<CommonObjects.TestMessageContents>>()
            ), Times.Once);
        }

        [Test]
        public async Task DispatchmessageResponseWithoutHandler()
        {
            // Setup: Create a new JSON RPC host without any pending requests
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            
            // If: I dispatch a response that doesn't have a pending request
            // Then: I should get an exception
            Assert.ThrowsAsync<MethodHandlerDoesNotExistException>(() => jh.DispatchMessage(CommonObjects.ResponseMessage));
        }
        
        [Test]
        public async Task DispatchMessageResponseWithHandler()
        {
            // Setup: Create a new JSON RPC host that has a pending request handler
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            var mockPendingRequest = new TaskCompletionSource<Message>();
            jh.pendingRequests.TryAdd(CommonObjects.MessageId, mockPendingRequest);
            
            // If: I dispatch a response
            await jh.DispatchMessage(CommonObjects.ResponseMessage);
            
            // Then: The task completion source should have completed with the message that was given
            await mockPendingRequest.Task.WithTimeout(TimeSpan.FromSeconds(1));
            Assert.AreEqual(CommonObjects.ResponseMessage, mockPendingRequest.Task.Result);
        }
        
        [Test]
        public async Task DispatchMessageEventWithoutHandler()
        {
            // Setup: Create a JSON RPC host without a request handler
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            
            // If: I dispatch a request that doesn't have a handler
            // Then: I should get an exception
            Assert.ThrowsAsync<MethodHandlerDoesNotExistException>(() => jh.DispatchMessage(CommonObjects.EventMessage));
        }
        
        [Test]
        public async Task DispatchMessageEventException()
        {
            // Setup: Create a JSON RPC host with a request handler that throws an unhandled exception every time
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            var mockHandler = new Mock<Func<CommonObjects.TestMessageContents, EventContext, Task>>();
            mockHandler.Setup(f => f(It.IsAny<CommonObjects.TestMessageContents>(), It.IsAny<EventContext>()))
                .Returns(Task.FromException(new Exception()));
            jh.SetAsyncEventHandler(CommonObjects.EventType, mockHandler.Object);

            // If: I dispatch a message whose handler throws
            // Then: I should get an exception
            Assert.ThrowsAsync<Exception>(() => jh.DispatchMessage(CommonObjects.EventMessage));
        }
        
        [TestCaseSource(nameof(DispatchMessageWithHandlerData))]
        public async Task DispatchMessageEventWithHandler(Task result)
        {
            // Setup: Create a JSON RPC host with an event handler setup
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            var mockHandler = new Mock<Func<CommonObjects.TestMessageContents, EventContext, Task>>();
            mockHandler.Setup(f => f(
                It.Is<CommonObjects.TestMessageContents>(m => m == CommonObjects.TestMessageContents.DefaultInstance),
                It.Is<EventContext>(ec => ec.messageQueue == jh.outputQueue)
            )).Returns(result);
            jh.SetAsyncEventHandler(CommonObjects.EventType, mockHandler.Object);
            
            // If: I dispatch an event
            await jh.DispatchMessage(CommonObjects.EventMessage);
            
            // Then: The event handler should have been called
            mockHandler.Verify(f => f(
                It.Is<CommonObjects.TestMessageContents>(m => m == CommonObjects.TestMessageContents.DefaultInstance),
                It.IsAny<EventContext>()
            ));
        }
        
        public static IEnumerable<object[]> DispatchMessageWithHandlerData
        {
            get
            {
                yield return new object[] {Task.FromResult(true)};                                                    // Successful completion
                yield return new object[] {Task.FromException(new TaskCanceledException())};                          // Cancelled result
                yield return new object[] {Task.FromException(new AggregateException(new TaskCanceledException()))};  // Cancelled somewhere inside
            }
        }
        
        #endregion

        #region ConsumeInput Loop Tests

        [Test]
        public async Task ConsumeInput()
        {
            // Setup:
            // ... Create a message reader that will return a message every time
            var mr = new Mock<MessageReader>(Stream.Null, null);
            var waitForRead = new TaskCompletionSource<bool>();
            mr.Setup(o => o.ReadMessage())
                .Callback(() => waitForRead.TrySetResult(true))
                .ReturnsAsync(CommonObjects.EventMessage);
            
            // ... Create a no-op event handler to handle the message from the message reader
            var noOpHandler = new Mock<Func<Message, Task>>();
            noOpHandler.Setup(f => f(It.IsAny<Message>())).Returns(Task.FromResult(true));
            
            // ... Wire up the event handler to a new JSON RPC host
            var jh = new JsonRpcHost(GetChannelBase(mr.Object, null).Object);
            jh.eventHandlers[CommonObjects.EventType.MethodName] = noOpHandler.Object;
            
            // If: 
            // ... I start the input consumption thread
            Task consumeInputTask = jh.ConsumeInput();
            
            // ... Wait for the handler to be called once, indicating the message was processed
            await waitForRead.Task.WithTimeout(TimeSpan.FromSeconds(1));
            
            // ... Stop the input consumption thread (the hard way) and wait for completion
            jh.cancellationTokenSource.Cancel();
            await consumeInputTask.WithTimeout(TimeSpan.FromSeconds(1));
            
            // Then: The event handler and read message should have been called at least once
            noOpHandler.Verify(f => f(It.IsAny<Message>()), Times.AtLeastOnce);
            mr.Verify(o => o.ReadMessage(), Times.AtLeastOnce);
        }

        [Test]
        public async Task ConsumeInputEndOfStream()
        {
            // Setup: Create a message reader that will throw an end of stream exception on read
            var mr = new Mock<MessageReader>(Stream.Null, null);
            mr.Setup(o => o.ReadMessage()).Returns(Task.FromException<Message>(new EndOfStreamException()));
            
            // If: I start the input consumption thread
            // Then: 
            // ... It should stop gracefully
            var jh = new JsonRpcHost(GetChannelBase(mr.Object, null).Object);
            await jh.ConsumeInput().WithTimeout(TimeSpan.FromSeconds(1));
            
            // ... The read message should have only been called once
            mr.Verify(o => o.ReadMessage(), Times.Once);
        }

        [Test]
        public async Task ConsumeInputException()
        {
            // Setup:
            // ... Create a message reader that will throw an exception on first read
            // ... throw an end of stream on second read
            var mr = new Mock<MessageReader>(Stream.Null, null);
            mr.SetupSequence(o => o.ReadMessage())
                .Returns(Task.FromException<Message>(new Exception()))
                .Returns(Task.FromException<Message>(new EndOfStreamException()));
            
            // If: I start the input consumption loop
            var jh = new JsonRpcHost(GetChannelBase(mr.Object, null).Object);
            await jh.ConsumeInput().WithTimeout(TimeSpan.FromSeconds(1));
            
            // Then:
            // ... Read message should have been called twice
            mr.Verify(o => o.ReadMessage(), Times.Exactly(2));
        }

        [Test]
        public async Task ConsumeInputRequestMethodNotFound()
        {
            // Setup: Create a message reader that will return a request with method that doesn't exist
            var mr = new Mock<MessageReader>(Stream.Null, null);
            mr.SetupSequence(o => o.ReadMessage())
                .ReturnsAsync(CommonObjects.RequestMessage)
                .Returns(Task.FromException<Message>(new EndOfStreamException()));

            // If: I start the input consumption loop
            var jh = new JsonRpcHost(GetChannelBase(mr.Object, null).Object);
            await jh.ConsumeInput().WithTimeout(TimeSpan.FromSeconds(1));

            // Then:
            // ... Read message should have been called twice
            mr.Verify(o => o.ReadMessage(), Times.Exactly(2));

            // ... 
            var outgoing = jh.outputQueue.ToArray();
            Assert.That(outgoing.Select(m => (m.MessageType, m.Id, m.Contents.Value<int>("code"))), Is.EqualTo(new[] { (MessageType.ResponseError, CommonObjects.MessageId, -32601) }), "There should be an outgoing message with the error");
        }

        [Test]
        public async Task ConsumeInputRequestException()
        {
            // Setup:
            // ... Create a message reader that will return a request
            var mr = new Mock<MessageReader>(Stream.Null, null);
            mr.SetupSequence(o => o.ReadMessage())
                .ReturnsAsync(CommonObjects.RequestMessage)
                .Returns(Task.FromException<Message>(new EndOfStreamException()));

            // ... Create a JSON RPC host and register the request handler to throw exception
            var jh = new JsonRpcHost(GetChannelBase(mr.Object, null).Object);
            var mockHandler = new Mock<Action<CommonObjects.TestMessageContents, RequestContext<CommonObjects.TestMessageContents>>>();
            mockHandler.Setup(m => m(It.IsAny<CommonObjects.TestMessageContents>(),
                    It.IsAny<RequestContext<CommonObjects.TestMessageContents>>()))
                .Throws(new Exception());
            jh.SetRequestHandler(CommonObjects.RequestType, mockHandler.Object);
            
            // If: I start the input consumption loop
            await jh.ConsumeInput().WithTimeout(TimeSpan.FromSeconds(1));
            
            // Then:
            // ... Read message should have been called twice
            mr.Verify(o => o.ReadMessage(), Times.Exactly(2));
            
            var outgoing = jh.outputQueue.ToArray();
            Assert.That(outgoing, Is.Empty, "outputQueue after ConsumeInput should not have any outgoing messages");
        }
        
        #endregion
        
        #region ConsumeOutput Loop Tests

        [Test]
        public async Task ConsumeOutput()
        {
            // Setup: 
            // ... Create a mock message writer
            var mw = new Mock<MessageWriter>(Stream.Null);
            mw.Setup(o => o.WriteMessage(CommonObjects.ResponseMessage)).Returns(Task.FromResult(true));
            
            // ... Create the JSON RPC host and add an item to the output queue
            var jh = new JsonRpcHost(GetChannelBase(null, mw.Object).Object);
            jh.outputQueue.Add(CommonObjects.ResponseMessage);
            jh.outputQueue.CompleteAdding();        // This will cause the thread to stop after processing the items
            
            // If: I start the output consumption thread
            Task consumeOutputTask = jh.ConsumeOutput();
            await consumeOutputTask.WithTimeout(TimeSpan.FromSeconds(1));
            
            // Then: The message writer should have been called once
            mw.Verify(o => o.WriteMessage(CommonObjects.ResponseMessage), Times.Once);
        }
        
        [Test]
        public async Task ConsumeOutputCancelled()
        {
            // NOTE: This test validates that the blocking collection breaks out when cancellation is requested
            
            // Setup: Create a mock message writer
            var mw = new Mock<MessageWriter>(Stream.Null);
            mw.Setup(o => o.WriteMessage(It.IsAny<Message>())).Returns(Task.FromResult(true));

            // If: 
            // ... I start the output consumption thread
            var jh = new JsonRpcHost(GetChannelBase(null, mw.Object).Object);
            Task consumeOuputTask = jh.ConsumeOutput();
            
            // ... and I stop the thread via cancellation and wait for completion
            jh.cancellationTokenSource.Cancel();
            await consumeOuputTask.WithTimeout(TimeSpan.FromSeconds(1));
            
            // Then: The message writer should not have been called
            mw.Verify(o => o.WriteMessage(It.IsAny<Message>()), Times.Never);
        }
        
        [Test]
        public async Task ConsumeOutputException()
        {
            // Setup: Create a mock message writer
            var mw = new Mock<MessageWriter>(Stream.Null);
            mw.Setup(o => o.WriteMessage(It.IsAny<Message>())).Returns(Task.FromResult(true));
            
            // If: I start the output consumption thread with a completed blocking collection
            var jh = new JsonRpcHost(GetChannelBase(null, mw.Object).Object);
            jh.outputQueue.CompleteAdding();
            await jh.ConsumeOutput().WithTimeout(TimeSpan.FromSeconds(1));
            
            // Then: The message writer should not have been called
            mw.Verify(o => o.WriteMessage(It.IsAny<Message>()), Times.Never);
        }
        
        #endregion
        
        #region Start/Stop Tests

        [Test]
        public async Task StartStop()
        {
            // Setup: Create mocked message reader and writer
            var mr = new Mock<MessageReader>(Stream.Null, null);
            var mw = new Mock<MessageWriter>(Stream.Null);
            
            // If: I start a JSON RPC host
            var cb = GetChannelBase(mr.Object, mw.Object);
            var jh = new JsonRpcHost(cb.Object);
            jh.Start();
            
            // Then: The channel protocol should have been
            cb.Verify(o => o.Start(), Times.Once);
            cb.Verify(o => o.WaitForConnection(), Times.Once);
            
            // If: I stop the JSON RPC host
            jh.Stop();
            
            // Then: The long running tasks should stop gracefully
            await Task.WhenAll(jh.consumeInputTask, jh.consumeOutputTask).WithTimeout(TimeSpan.FromSeconds(1));
            cb.Verify(o => o.Stop(), Times.Once);
        }

        [Test]
        public async Task StartMultiple()
        {
            // Setup: Create mocked message reader and writer
            var mr = new Mock<MessageReader>(Stream.Null, null);
            var mw = new Mock<MessageWriter>(Stream.Null);
            
            // If:
            // ... I start a JSON RPC host
            var cb = GetChannelBase(mr.Object, mw.Object);
            var jh = new JsonRpcHost(cb.Object);
            jh.Start();
            
            // ... And I start it again
            // Then: I should get an exception
            Assert.Throws<InvalidOperationException>(() => jh.Start());
            
            // Cleanup: Stop the JSON RPC host
            jh.Stop();
            await Task.WhenAll(jh.consumeInputTask, jh.consumeOutputTask).WithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void StopMultiple()
        {
            // Setup: Create json rpc host and start it
            var mr = new Mock<MessageReader>(Stream.Null, null);
            var mw = new Mock<MessageWriter>(Stream.Null);
            var jh = new JsonRpcHost(GetChannelBase(mr.Object, mw.Object).Object);
            jh.Start();

            // If: I stop the JSON RPC host after stopping it
            // Then: I should get an exception
            jh.Stop();
            Assert.Throws<InvalidOperationException>(() => jh.Stop());
        }

        [Test]
        public void StopBeforeStarting()
        {
            // If: I stop the JSON RPC host without starting it first
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.Throws<InvalidOperationException>(() => jh.Stop());
        }

        [Test]
        public async Task WaitForExit()
        {
            // Setup: Create json rpc host and start it
            var mr = new Mock<MessageReader>(Stream.Null, null);
            var mw = new Mock<MessageWriter>(Stream.Null);
            var jh = new JsonRpcHost(GetChannelBase(mr.Object, mw.Object).Object);
            jh.Start();
            
            // If: I wait for JSON RPC host to exit and stop it
            // NOTE: We are wrapping this execution in a task to make sure we can properly stop the host
            Task waitForExit = Task.Run(() => { jh.WaitForExit(); });
            jh.Stop();
            
            // Then: The host should be stopped
            await waitForExit.WithTimeout(TimeSpan.FromSeconds(1));
        }
        
        [Test]
        public void WaitForExitNotStarted()
        {
            // If: I wait for exit on the JSON RPC host without starting it
            // Then: I should get an exception
            var jh = new JsonRpcHost(GetChannelBase(null, null).Object);
            Assert.Throws<InvalidOperationException>(() => jh.WaitForExit());
        }
        
        #endregion
            
        private static Mock<ChannelBase> GetChannelBase(MessageReader reader, MessageWriter writer, bool isConnected = false)
        {           
            var cb = new Mock<ChannelBase>();
            cb.Object.MessageReader = reader;
            cb.Object.MessageWriter = writer;
            cb.Object.IsConnected = isConnected;
            cb.Setup(o => o.Start());
            cb.Setup(o => o.Stop());
            cb.Setup(o => o.WaitForConnection()).Returns(Task.FromResult(true));

            return cb;
        }
    }
}
