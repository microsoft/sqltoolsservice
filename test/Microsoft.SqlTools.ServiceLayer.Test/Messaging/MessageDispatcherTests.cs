//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Messaging
{
    public class MessageDispatcherTests
    {
        [Fact]
        public void SetRequestHandlerWithOverrideTest()
        {           
            RequestType<int, int> requestType = RequestType<int, int>.Create("test/requestType");            
            var dispatcher = new MessageDispatcher(new Mock<ChannelBase>().Object);
            dispatcher.SetRequestHandler<int, int>(
                requestType,
                (i, j) => 
                { 
                    return Task.FromResult(0);
                },
                true);
            Assert.True(dispatcher.requestHandlers.Count > 0);            
        }

        [Fact]
        public void SetEventHandlerTest()
        {           
            EventType<int> eventType = EventType<int>.Create("test/eventType");            
            var dispatcher = new MessageDispatcher(new Mock<ChannelBase>().Object);
            dispatcher.SetEventHandler<int>(
                eventType,
                (i, j) => 
                { 
                    return Task.FromResult(0);
                });
            Assert.True(dispatcher.eventHandlers.Count > 0);            
        }

        [Fact]
        public void SetEventHandlerWithOverrideTest()
        {           
            EventType<int> eventType = EventType<int>.Create("test/eventType");            
            var dispatcher = new MessageDispatcher(new Mock<ChannelBase>().Object);
            dispatcher.SetEventHandler<int>(
                eventType,
                (i, j) => 
                { 
                    return Task.FromResult(0);
                },
                true);
            Assert.True(dispatcher.eventHandlers.Count > 0);            
        }

        [Fact]
        public void OnListenTaskCompletedFaultedTaskTest()
        {           
            Task t = null;
            
            try
            {
                t = Task.Run(() => 
                { 
                    throw new Exception();
                });            
                t.Wait();
            }
            catch
            {                
            }
            finally
            {
                bool handlerCalled = false;
                var dispatcher = new MessageDispatcher(new Mock<ChannelBase>().Object);
                dispatcher.UnhandledException += (s, e) => handlerCalled = true;       
                dispatcher.OnListenTaskCompleted(t);
                Assert.True(handlerCalled);   
            }
        }
    }
}