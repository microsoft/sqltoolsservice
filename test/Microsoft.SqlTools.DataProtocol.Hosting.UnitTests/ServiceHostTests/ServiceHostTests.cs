//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.DataProtocol.Contracts;
using Microsoft.SqlTools.DataProtocol.Contracts.Hosting;
using Microsoft.SqlTools.DataProtocol.Hosting.Channels;
using Microsoft.SqlTools.DataProtocol.Hosting.Protocol;
using Microsoft.SqlTools.DataProtocol.Hosting.UnitTests.ProtocolTests;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.DataProtocol.Hosting.UnitTests.ServiceHostTests
{
    public class ServiceHostTests
    {
        #region Construction Tests

        [Fact]
        public void ServiceHostConstructDefaultServer()
        {
            // If: I construct a default server service host
            var sh = ServiceHost.CreateDefaultServer(new ProviderDetails(), new LanguageServiceCapabilities());
            
            // Then: The underlying json rpc host should be using the stdio server channel
            var jh = sh.jsonRpcHost as JsonRpcHost;
            Assert.NotNull(jh);
            Assert.IsType<StdioServerChannel>(jh.protocolChannel);
            Assert.False(jh.protocolChannel.IsConnected);
        }
        
        [Theory]
        [MemberData(nameof(ServiceHostNullParameterData))]
        public void ServiceHostNullParameter(ChannelBase cb, ProviderDetails pd, LanguageServiceCapabilities lsc)
        {
            // If: I create a service host with missing parameters
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => new ServiceHost(cb, pd, lsc));
        }

        public static IEnumerable<object[]> ServiceHostNullParameterData
        {
            get
            {
                yield return new object[] {null, new ProviderDetails(), new LanguageServiceCapabilities()};
                yield return new object[] {new Mock<ChannelBase>().Object, null, new LanguageServiceCapabilities()};
                yield return new object[] {new Mock<ChannelBase>().Object, new ProviderDetails(), null};
            }
        }

        #endregion
        
        #region IServiceHost Tests

        [Fact]
        public void RegisterInitializeTask()
        {
            // Setup: Create mock initialize handler
            var mockHandler = new Mock<Func<InitializeParams, IEventSender, Task>>().Object;
            
            // If: I register a couple initialize tasks with the service host
            var sh = GetServiceHost();
            sh.RegisterInitializeTask(mockHandler);
            sh.RegisterInitializeTask(mockHandler);
            
            // Then: There should be two initialize tasks registered
            Assert.Equal(2, sh.initializeCallbacks.Count);
            Assert.True(sh.initializeCallbacks.SequenceEqual(new[] {mockHandler, mockHandler}));
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
            sh.SendEvent(Common.EventType, Common.TestMessageContents.DefaultInstance);
            
            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SendEvent(Common.EventType, Common.TestMessageContents.DefaultInstance), Times.Once);
        }

        [Fact]
        public async Task SendRequest()
        {
            // If: I send a request
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            await sh.SendRequest(Common.RequestType, Common.TestMessageContents.DefaultInstance);
            
            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SendRequest(Common.RequestType, Common.TestMessageContents.DefaultInstance), Times.Once);
        }

        [Fact]
        public void SetAsyncEventHandler()
        {
            // If: I set an event handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetAsyncEventHandler(Common.EventType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetAsyncEventHandler(Common.EventType, null, true), Times.Once);
        }
        
        [Fact]
        public void SetSyncEventHandler()
        {
            // If: I set an event handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetEventHandler(Common.EventType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetEventHandler(Common.EventType, null, true), Times.Once);
        }
        
        [Fact]
        public void SetAsyncRequestHandler()
        {
            // If: I set a request handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetAsyncRequestHandler(Common.RequestType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetAsyncRequestHandler(Common.RequestType, null, true), Times.Once);
        }
        
        [Fact]
        public void SetSyncRequestHandler()
        {
            // If: I set a request handler
            var jh = GetJsonRpcHostMock();
            var sh = GetServiceHost(jh.Object);
            sh.SetRequestHandler(Common.RequestType, null, true);

            // Then: The underlying json rpc host should have handled it
            jh.Verify(o => o.SetRequestHandler(Common.RequestType, null, true), Times.Once);
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
            
            // ... There should be handlers registered for the built in requests
            jh.Verify(o => o.SetEventHandler(ExitNotification.Type, sh.HandleExitNotification, true), Times.Once);
            jh.Verify(o => o.SetAsyncRequestHandler(InitializeRequest.Type, sh.HandleInitializeRequest, true), Times.Once);
            jh.Verify(o => o.SetAsyncRequestHandler(ShutdownRequest.Type, sh.HandleShutdownRequest, true), Times.Once);
            jh.Verify(o => o.SetRequestHandler(VersionRequest.Type, sh.HandleVersionRequest, true), Times.Once);
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
            var mockHandler = new Mock<Func<InitializeParams, IEventSender, Task>>();
            mockHandler.Setup(f => f(It.IsAny<InitializeParams>(), It.IsAny<IEventSender>()))
                .Returns(Task.FromResult(true));
            
            var sh = GetServiceHost();
            sh.RegisterInitializeTask(mockHandler.Object);
            sh.RegisterInitializeTask(mockHandler.Object);
            
            // ... Create a mock request context that will handle sending a result
            // TODO: Replace with the event flow validation
            var initParams = new InitializeParams();
            var bc = new BlockingCollection<Message>();
            var mockContext = new RequestContext<InitializeResult>(Message.CreateRequest(InitializeRequest.Type, Common.MessageId, initParams), bc);

            // If: I handle an initialize request
            await sh.HandleInitializeRequest(initParams, mockContext);
            
            // Then: 
            // ... The mock handler should have been called twice
            mockHandler.Verify(h => h(initParams, mockContext), Times.Exactly(2));
            
            // ... There should have been a response sent
            Assert.Equal(1, bc.ToArray().Length);
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
            var mockContext = new RequestContext<object>(Message.CreateRequest(ShutdownRequest.Type, Common.MessageId, shutdownParams), bc);
            
            // If: I handle a shutdown request
            await sh.HandleShutdownRequest(shutdownParams, mockContext);
            
            // Then:
            // ... The mock handler should have been called twice
            mockHandler.Verify(h => h(shutdownParams, mockContext));
            
            // ... There should have been a response sent
            Assert.Equal(1, bc.ToArray().Length); 
        }

        [Fact]
        public void HandleVersionRequest()
        {
            // Setup: Create a mock request that will handle sending a result
            // TODO: Replace with the event flow validation
            var versionParams = new object();
            var bc = new BlockingCollection<Message>();
            var mockContext = new RequestContext<string>(Message.CreateRequest(VersionRequest.Type, Common.MessageId, versionParams), bc);
            
            // If: I handle a version request
            var sh = GetServiceHost();
            sh.HandleVersionRequest(versionParams, mockContext);
            
            // Then: There should have been a response sent
            Assert.Equal(1, bc.ToArray().Length);
        }
        
        #endregion
        
        private static readonly ProviderDetails ProviderDetails = new ProviderDetails();
        private static readonly LanguageServiceCapabilities Capabilities = new LanguageServiceCapabilities();
        
        private static ServiceHost GetServiceHost(IJsonRpcHost jsonRpcHost = null)
        {
            return new ServiceHost(ProviderDetails, Capabilities)
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
