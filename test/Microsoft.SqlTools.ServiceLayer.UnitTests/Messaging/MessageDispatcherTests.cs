//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.Utility;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    public class MessageDispatcherTests
    {
        private sealed class TestMessageDispatcher : MessageDispatcher
        {
            public TestMessageDispatcher(ChannelBase protocolChannel) : base(protocolChannel)
            {
            }

            public Task DispatchForTest(Message message, MessageWriter messageWriter)
            {
                return DispatchMessage(message, messageWriter);
            }
        }

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
            int numOfRequests = 10;
            int msForEachRequest = 1000;
            // Without parallel processing, this should take around numOfRequests * msForEachRequest ms to finish.
            // With parallel process, this should take around 1 * msForEachRequest ms to finish in theory (with perfect parallelism).
            // The diff should in theory be around (numOfRequests - 1) * msForEachRequest ms.
            // In order to make this test stable on machines with poor hardware / few logical cores, 
            // we loose the assertion by only checking parallel process being faster than sequential processing.
            Assert.IsTrue(GetTimeToHandleRequests(false, numOfRequests, msForEachRequest) > GetTimeToHandleRequests(true, numOfRequests, msForEachRequest));
        }

        [Test]
        public async Task DispatchMessageAddsOperationContextToRequestHandler()
        {
            TestLogger testLogger = new TestLogger()
            {
                TraceSource = nameof(DispatchMessageAddsOperationContextToRequestHandler),
                TracingLevel = SourceLevels.Verbose,
            };
            testLogger.Initialize();
            RequestType<JObject, JObject> requestType = RequestType<JObject, JObject>.Create("query/executeString");
            var dispatcher = new TestMessageDispatcher(new Mock<ChannelBase>().Object);
            LogOperationContext observedContext = null;

            dispatcher.SetRequestHandler<JObject, JObject>(
                requestType,
                (parameters, context) =>
                {
                    observedContext = Logger.CurrentOperationContext;
                    Logger.Verbose("Handler saw operation context");
                    return Task.CompletedTask;
                });

            await dispatcher.DispatchForTest(
                Message.Request(
                    "123",
                    requestType.MethodName,
                    JObject.FromObject(new { ownerUri = "file:///c:/query.sql" })),
                new MessageWriter(new MemoryStream(), new V8MessageSerializer()));

            Logger.Flush();
            string contents = testLogger.LogContents;
            Assert.NotNull(observedContext);
            Assert.AreEqual("queryExecution", observedContext.Service);
            Assert.AreEqual("query/executeString", observedContext.RpcMethod);
            Assert.AreEqual("123", observedContext.RpcId);
            Assert.AreEqual("Request", observedContext.RpcType);
            Assert.NotNull(observedContext.FlowId);
            Assert.True(contents.Contains("service:queryExecution rpcMethod:query/executeString"));
            Assert.True(contents.Contains("Operation start"));
            Assert.True(contents.Contains("status:success"));
            Assert.True(contents.Contains("Handler saw operation context"));
            Assert.Null(Logger.CurrentOperationContext);
            testLogger.Cleanup();
        }

        [Test]
        public async Task DispatchMessagePreservesOperationContextForParallelHandler()
        {
            TestLogger testLogger = new TestLogger()
            {
                TraceSource = nameof(DispatchMessagePreservesOperationContextForParallelHandler),
                TracingLevel = SourceLevels.Verbose,
            };
            testLogger.Initialize();
            RequestType<JObject, JObject> requestType = RequestType<JObject, JObject>.Create("connection/connect");
            var dispatcher = new TestMessageDispatcher(new Mock<ChannelBase>().Object)
            {
                ParallelMessageProcessing = true,
            };
            var observedContext = new TaskCompletionSource<LogOperationContext>();

            dispatcher.SetRequestHandler<JObject, JObject>(
                requestType,
                (parameters, context) =>
                {
                    observedContext.SetResult(Logger.CurrentOperationContext);
                    return Task.CompletedTask;
                },
                false,
                true);

            await dispatcher.DispatchForTest(
                Message.Request(
                    "456",
                    requestType.MethodName,
                    JObject.FromObject(new { ownerUri = "file:///c:/connect.sql" })),
                new MessageWriter(new MemoryStream(), new V8MessageSerializer()));

            var completedTask = await Task.WhenAny(observedContext.Task, Task.Delay(5_000));
            Assert.AreSame(observedContext.Task, completedTask);
            Assert.AreEqual("connection", observedContext.Task.Result.Service);
            Assert.AreEqual("connection/connect", observedContext.Task.Result.RpcMethod);
            await WaitForLogFileContent(Logger.LogFileFullPath, "status:success");
            Assert.Null(Logger.CurrentOperationContext);
            testLogger.Cleanup();
        }

        private static async Task WaitForLogFileContent(string logFilePath, string expectedContent)
        {
            for (int i = 0; i < 50; i++)
            {
                Logger.Flush();
                if (File.Exists(logFilePath) && ReadAllTextShared(logFilePath).Contains(expectedContent))
                {
                    return;
                }

                await Task.Delay(100);
            }

            Assert.Fail($"Timed out waiting for log content: {expectedContent}");
        }

        private static string ReadAllTextShared(string logFilePath)
        {
            using (var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
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
                // simulate a slow sync call
                Thread.Sleep(msForEachRequest / 2);
                // simulate a delay async call
                await Task.Delay(msForEachRequest / 2);
                await unfinishedRequestCount.WaitAsync();
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
