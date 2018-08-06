//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.DataProtocol.Contracts;
using Microsoft.SqlTools.Hosting.Channels;
using Microsoft.SqlTools.Hosting.Contracts;
using Microsoft.SqlTools.Hosting.Contracts.Internal;
using Microsoft.SqlTools.Hosting.Protocol;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.SqlTools.Hosting.UnitTests.ServiceHostTests
{
    public class ServiceHostTests
    {
        #region Construction Tests

        [Fact]
        public void ServiceHostConstructDefaultServer()
        {
            // If: I construct a default server service host
            var sh = ServiceHost.CreateDefaultServer();
            
            // Then: The underlying json rpc host should be using the stdio server channel
            var jh = sh.jsonRpcHost as JsonRpcHost;
            Assert.NotNull(jh);
            Assert.IsType<StdioServerChannel>(jh.protocolChannel);
            Assert.False(jh.protocolChannel.IsConnected);
        }
        
        [Fact]
        public void ServiceHostNullParameter()
        {
            // If: I create a service host with missing parameters
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => new ServiceHost(null));
        }

        #endregion
        
        #region IServiceHost Tests

        [Fact]
        public void RegisterInitializeTask()
        {
            // Setup: Create mock initialize handler
            var mockHandler = new Mock<Func<InitializeParameters, IEventSender, Task>>().Object;
            
            // If: I register a couple initialize tasks with the service host
            var sh = GetServiceHost();
            sh.RegisterInitializeTask(mockHandler);
            sh.RegisterInitializeTask(mockHandler);
            
            // Then: There should be two initialize tasks registered
            Assert.Equal(2, sh.initCallbacks.Count);
            Assert.True(sh.initCallbacks.SequenceEqual(new[] {mockHandler, mockHandler}));
        }

        [Fact]
        public void RegisterInitializeTaskNullHandler()
        {
            // If: I register a null initialize task
            // Then: I should get an exception
            var sh = GetServiceHost();
            Assert.Throws<ArgumentNullException>(() => sh.RegisterInitializeTask(null));
        }
        
        [Fact]
        public void RegisterShutdownTask()
        {
            // Setup: Create mock initialize handler
            var mockHandler = new Mock<Func<object, IEventSender, Task>>().Object;
            
            // If: I register a couple shutdown tasks with the service host
            var sh = GetServiceHost();
            sh.RegisterShutdownTask(mockHandler);
            sh.RegisterShutdownTask(mockHandler);
            
            // Then: There should be two initialize tasks registered
            Assert.Equal(2, sh.shutdownCallbacks.Count);
            Assert.True(sh.shutdownCallbacks.SequenceEqual(new[] {mockHandler, mockHandler}));
        }

        [Fact]
        public void RegisterShutdownTaskNullHandler()
        {
            // If: I register a null initialize task
            // Then: I should get an exception
            var sh = GetServiceHost();
            Assert.Throws<ArgumentNullException>(() => sh.RegisterShutdownTask(null));
        }
        
        #endregion

        #region IJsonRpcHost Tests

        [Fact]
        public void SendEvent()
        {
            // If: I send an event
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SendEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance);
            
            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SendEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance), Times.Once);
        }

        [Fact]
        public async Task SendRequest()
        {
            // If: I send a request
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            await sh.SendRequest(CommonObjects.RequestType, CommonObjects.TestMessageContents.DefaultInstance);
            
            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SendRequest(CommonObjects.RequestType, CommonObjects.TestMessageContents.DefaultInstance), Times.Once);
        }

        [Fact]
        public void SetAsyncEventHandler()
        {
            // If: I set an event handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetAsyncEventHandler(CommonObjects.EventType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetAsyncEventHandler(CommonObjects.EventType, null, true), Times.Once);
        }
        
        [Fact]
        public void SetSyncEventHandler()
        {
            // If: I set an event handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetEventHandler(CommonObjects.EventType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetEventHandler(CommonObjects.EventType, null, true), Times.Once);
        }
        
        [Fact]
        public void SetAsyncRequestHandler()
        {
            // If: I set a request handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetAsyncRequestHandler(CommonObjects.RequestType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetAsyncRequestHandler(CommonObjects.RequestType, null, true), Times.Once);
        }
        
        [Fact]
        public void SetSyncRequestHandler()
        {
            // If: I set a request handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetRequestHandler(CommonObjects.RequestType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetRequestHandler(CommonObjects.RequestType, null, true), Times.Once);
        }

        [Fact]
        public void Start()
        {
            // If: I start a service host
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.Start();
            
            // Then:
            // ... The underlying json rpc host should have handled it
            jh.Verify(o => o.Start(), Times.Once);
        }

        [Fact]
        public void Stop()
        {
            // If: I stop a service host
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.Stop();
            
            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.Stop(), Times.Once);
        }

        [Fact]
        public void WaitForExit()
        {
            // If: I wait for service host to exit
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.WaitForExit();
            
            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.WaitForExit());
        }
        
        #endregion
        
        #region Request Handling Tests

        [Fact]
        public void HandleExitNotification()
        {
            // If: I handle an exit notification
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.HandleExitNotification(null, null);
            
            // Then: The json rpc host should have been stopped
            jh.Verify(o => o.Stop(), Times.Once);
        }

        [Fact]
        public async Task HandleInitializeRequest()
        {
            // Setup:
            // ... Create an initialize handler and register it in the service host
            var mockHandler = new Mock<Func<InitializeParameters, IEventSender, Task>>();
            mockHandler.Setup(f => f(It.IsAny<InitializeParameters>(), It.IsAny<IEventSender>()))
                .Returns(Task.FromResult(true));

            var sh = GetServiceHost();
            sh.RegisterInitializeTask(mockHandler.Object);
            sh.RegisterInitializeTask(mockHandler.Object);
            
            // ... Set a dummy value to return as the initialize response
            var ir = new InitializeResponse();
            
            // ... Create a mock request that will handle sending a result
            // TODO: Replace with event flow validation
            var initParams = new InitializeParameters();
            var bc = new BlockingCollection<Message>();
            var mockContext = new RequestContext<InitializeResponse>(Message.CreateRequest(InitializeRequest.Type, CommonObjects.MessageId, initParams), bc);
            
            // If: I handle an initialize request
            await sh.HandleInitializeRequest(initParams, mockContext);
            
            // Then:
            // ... The mock handler should have been called twice
            mockHandler.Verify(h => h(initParams, mockContext), Times.Exactly(2));
            
            // ... There should have been a response sent
            var outgoing = bc.ToArray();
            Assert.Equal(1, outgoing.Length);
            Assert.Equal(CommonObjects.MessageId, outgoing[0].Id);
            Assert.Equal(JToken.FromObject(ir), JToken.FromObject(ir));
        }
        
        [Fact]
        public async Task HandleShutdownRequest()
        {
            // Setup:
            // ... Create a shutdown handler and register it in the service host
            var mockHandler = new Mock<Func<object, IEventSender, Task>>();
            mockHandler.Setup(f => f(It.IsAny<object>(), It.IsAny<IEventSender>()))
                .Returns(Task.FromResult(true));
            
            var sh = GetServiceHost();
            sh.RegisterShutdownTask(mockHandler.Object);
            sh.RegisterShutdownTask(mockHandler.Object);
            
            // ... Create a mock request that will handle sending a result
            // TODO: Replace with the event flow validation
            var shutdownParams = new object();
            var bc = new BlockingCollection<Message>();
            var mockContext = new RequestContext<object>(Message.CreateRequest(ShutdownRequest.Type, CommonObjects.MessageId, shutdownParams), bc);
            
            // If: I handle a shutdown request
            await sh.HandleShutdownRequest(shutdownParams, mockContext);
            
            // Then:
            // ... The mock handler should have been called twice
            mockHandler.Verify(h => h(shutdownParams, mockContext), Times.Exactly(2));
            
            // ... There should have been a response sent
            Assert.Equal(1, bc.ToArray().Length); 
        }
        
        #endregion
        
        private static ServiceHost GetServiceHost(IJsonRpcHost jsonRpcHost = null)
        {
            return new ServiceHost
            {
                jsonRpcHost = jsonRpcHost
            };
        }

        private static Mock<IJsonRpcHost> GetJsonRpcHostMock()
        {
            var anyEventType = It.IsAny<EventType<object>>();
            var anyRequestType = It.IsAny<RequestType<object, object>>();
            var anyParams = It.IsAny<object>();
            
            var mock = new Mock<IJsonRpcHost>();
            mock.Setup(jh => jh.SendEvent(anyEventType, anyParams));
            mock.Setup(jh => jh.SendRequest(anyRequestType, anyParams)).ReturnsAsync(null);
            mock.Setup(jh => jh.SetAsyncEventHandler(anyEventType, It.IsAny<Func<object, EventContext, Task>>(), It.IsAny<bool>()));
            mock.Setup(jh => jh.SetEventHandler(anyEventType, It.IsAny<Action<object, EventContext>>(), It.IsAny<bool>()));
            mock.Setup(jh => jh.SetAsyncRequestHandler(anyRequestType, It.IsAny<Func<object, RequestContext<object>, Task>>(), It.IsAny<bool>()));
            mock.Setup(jh => jh.SetRequestHandler(anyRequestType, It.IsAny<Action<object, RequestContext<object>>>(), It.IsAny<bool>()));
            mock.Setup(jh => jh.Start());
            mock.Setup(jh => jh.Stop());
            mock.Setup(jh => jh.WaitForExit());

            return mock;
        }
    }
}
