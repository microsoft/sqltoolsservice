//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    /// <summary>
    /// Regression coverage for protocol stalls. Each test controls the exact wait rather than
    /// depending on machine load or ThreadPool sizing.
    /// </summary>
    public class ProtocolStallTests
    {
        private sealed class TestMessageReader : MessageReader
        {
            internal Channel<Message> Messages { get; } = Channel.CreateUnbounded<Message>();

            public override async Task<Message> ReadMessage()
            {
                try
                {
                    return await this.Messages.Reader.ReadAsync();
                }
                catch (ChannelClosedException)
                {
                    throw new EndOfStreamException();
                }
            }
        }

        private sealed class TestChannel : ChannelBase
        {
            internal TestMessageReader Input { get; } = new TestMessageReader();

            internal Stream Output { get; set; } = new MemoryStream();

            protected override void Initialize(
                IMessageSerializer messageSerializer,
                Stream inputStream = null,
                Stream outputStream = null)
            {
                this.MessageReader = this.Input;
                this.MessageWriter = new MessageWriter(this.Output, messageSerializer);
                this.IsConnected = true;
            }

            public override Task WaitForConnection() => Task.CompletedTask;

            protected override void Shutdown()
            {
                this.IsConnected = false;
                this.Input.Messages.Writer.TryComplete();
            }
        }

        private sealed class FailingOutputStream : MemoryStream
        {
            public override Task WriteAsync(
                byte[] buffer,
                int offset,
                int count,
                CancellationToken cancellationToken)
                => Task.FromException(new IOException("Injected output failure"));
        }

        private static TaskCompletionSource<bool> NewSignal()
            => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        [Test]
        [Timeout(10_000)]
        public async Task ParallelProcessingHonorsConfiguredLimit()
        {
            var channel = new TestChannel();
            channel.Start(MessageProtocolType.LanguageServer);
            var dispatcher = new MessageDispatcher(channel)
            {
                ParallelMessageProcessing = true,
                ParallelMessageProcessingLimit = 1,
            };
            var firstStarted = NewSignal();
            var allCompleted = NewSignal();
            var release = NewSignal();
            int active = 0;
            int maximumActive = 0;
            int completed = 0;

            dispatcher.SetRequestHandler(
                RequestType<int, int>.Create("test/parallel-limit"),
                async (_, _) =>
                {
                    int nowActive = Interlocked.Increment(ref active);
                    int observed;
                    while (nowActive > (observed = Volatile.Read(ref maximumActive)))
                    {
                        Interlocked.CompareExchange(ref maximumActive, nowActive, observed);
                    }

                    firstStarted.TrySetResult(true);
                    await release.Task;
                    Interlocked.Decrement(ref active);
                    if (Interlocked.Increment(ref completed) == 8)
                    {
                        allCompleted.TrySetResult(true);
                    }
                },
                overrideExisting: false,
                isParallelProcessingSupported: true);

            try
            {
                dispatcher.Start();
                for (int i = 0; i < 8; i++)
                {
                    channel.Input.Messages.Writer.TryWrite(
                        Message.Request(i + 1, "test/parallel-limit", null));
                }

                await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
                await Task.Delay(250);
                Assert.That(Volatile.Read(ref maximumActive), Is.EqualTo(1),
                    "the configured limit must bound active handlers during a burst");

                release.TrySetResult(true);
                await allCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            finally
            {
                release.TrySetResult(true);
                dispatcher.Stop();
                channel.Stop();
            }
        }

        [Test]
        [Timeout(10_000)]
        public async Task SerialHandlerCanReceiveResponseToOutboundRequest()
        {
            var channel = new TestChannel();
            var endpoint = new ProtocolEndpoint(channel, MessageProtocolType.LanguageServer);
            endpoint.Initialize();
            var outboundRequestStarted = NewSignal();
            var handlerCompleted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientRequest = RequestType<int, int>.Create("test/client-round-trip");

            endpoint.SetRequestHandler(
                RequestType<int, int>.Create("test/serial-handler"),
                async (_, _) =>
                {
                    Task<int> response = endpoint.SendRequest(clientRequest, 7, waitForResponse: true);
                    outboundRequestStarted.TrySetResult(true);
                    handlerCompleted.TrySetResult(await response);
                },
                isParallelProcessingSupported: true);

            try
            {
                // ParallelMessageProcessing deliberately remains false. The read loop must still
                // be able to consume responses needed by the serialized handler.
                endpoint.MessageDispatcher.Start();
                channel.Input.Messages.Writer.TryWrite(
                    Message.Request(100, "test/serial-handler", null));
                await outboundRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

                channel.Input.Messages.Writer.TryWrite(
                    Message.Response(1, clientRequest.MethodName, JToken.FromObject(8)));

                Assert.That(
                    await handlerCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2)),
                    Is.EqualTo(8));
            }
            finally
            {
                endpoint.MessageDispatcher.Stop();
                channel.Stop();
            }
        }

        [Test]
        [Timeout(10_000)]
        public async Task SendEventPropagatesPostedWriteFailureToCaller()
        {
            var channel = new TestChannel { Output = new FailingOutputStream() };
            var endpoint = new ProtocolEndpoint(channel, MessageProtocolType.LanguageServer);
            endpoint.Initialize();
            var dispatcherReady = NewSignal();
            endpoint.SetRequestHandler(
                RequestType<int, int>.Create("test/dispatcher-ready"),
                (_, _) =>
                {
                    dispatcherReady.TrySetResult(true);
                    return Task.CompletedTask;
                });

            try
            {
                endpoint.MessageDispatcher.Start();
                channel.Input.Messages.Writer.TryWrite(
                    Message.Request(1, "test/dispatcher-ready", null));
                await dispatcherReady.Task.WaitAsync(TimeSpan.FromSeconds(2));

                Task send = endpoint.SendEvent(EventType<int>.Create("test/failing-event"), 1);
                Assert.That(
                    async () => await send.WaitAsync(TimeSpan.FromSeconds(2)),
                    Throws.TypeOf<IOException>());
            }
            finally
            {
                endpoint.MessageDispatcher.Stop();
                channel.Stop();
            }
        }

        [Test]
        [Timeout(10_000)]
        public void OutboundRequestTimeoutRemovesPendingRequest()
        {
            var channel = new TestChannel();
            var endpoint = new ProtocolEndpoint(channel, MessageProtocolType.LanguageServer)
            {
                PendingRequestTimeout = TimeSpan.FromMilliseconds(100),
            };
            endpoint.Initialize();

            Task<int> request = endpoint.SendRequest(
                RequestType<int, int>.Create("test/no-response"),
                1,
                waitForResponse: true);

            Assert.That(async () => await request, Throws.TypeOf<TimeoutException>());
            Assert.That(endpoint.PendingRequestCount, Is.Zero,
                "a timed-out response must not remain in the pending-request registry");
            channel.Stop();
        }
    }
}
