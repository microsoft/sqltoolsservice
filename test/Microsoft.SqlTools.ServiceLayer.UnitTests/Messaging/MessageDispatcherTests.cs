//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    public class MessageDispatcherTests
    {
        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public void ParallelMessageProcessingTest()
        {
            int numOfRequests = 11;
            int msForEachRequest = 1000;
            // Without parallel processing, this should take around numOfRequests * msForEachRequest ms to finish.
            // With parallel process, this should take around 1 * msForEachRequest ms to finish.
            // The diff should be greater than (numOfRequests - 1) * msForEachRequest ms.
            Assert.IsTrue(GetTimeToHandleRequests(false, numOfRequests, msForEachRequest) - GetTimeToHandleRequests(true, numOfRequests, msForEachRequest) > msForEachRequest * (numOfRequests - 1));
        }


        /// <summary>
        /// Gets the time to handle certain amount of requests in ms
        /// </summary>
        /// <param name="parallelMessageProcessing">Wheater to enable parallel processing</param>
        /// <param name="numOfRequests">num of requests to handle</param>
        /// <param name="msForEachRequest">rough time taken to finish each reqeust in ms</param>
        /// <returns></returns>
        private long GetTimeToHandleRequests(bool parallelMessageProcessing, int numOfRequests, int msForEachRequest)
        {
            RequestType<int, int> requestType = RequestType<int, int>.Create("test/requestType");
            var mockChannel = new Mock<ChannelBase>();
            SemaphoreSlim unfinishedRequestCount = new SemaphoreSlim(numOfRequests);
            bool okayToEnd = false;
            mockChannel.Setup(c => c.MessageReader.ReadMessage())
                .Returns(Task.FromResult(Message.Request("1", "test/requestType", null)));
            var dispatcher = new MessageDispatcher(mockChannel.Object);
            dispatcher.ParallelMessageProcessing = parallelMessageProcessing;
            Stopwatch stopwatch = Stopwatch.StartNew();
            var handler = async (int _, RequestContext<int> _) =>
            {
                Thread.Sleep(msForEachRequest);
                unfinishedRequestCount.Wait();
                if (unfinishedRequestCount.CurrentCount == 0)
                {
                    // cut off when we reach numOfRequests
                    stopwatch.Stop();
                    okayToEnd = true;
                }
                await Task.CompletedTask;
            };

            dispatcher.SetRequestHandler(requestType, handler, false, true);
            dispatcher.Start();

            while (true)
            {
                if (okayToEnd)
                {
                    // wait until we finish handling the required amount of requests
                    break;
                }
                Thread.Sleep(1000);
            }

            dispatcher.Stop();
            return stopwatch.ElapsedMilliseconds;
        }


    }
}