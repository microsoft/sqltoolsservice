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
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    public class MessageDispatcherParallelLimitTests
    {
        private const string ParallelMethod = "test/parallel";
        private const string InlineMethod = "test/inline";
        private const int RequestCount = 8;

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

            protected override void Initialize(
                IMessageSerializer messageSerializer,
                Stream inputStream = null,
                Stream outputStream = null)
            {
                this.MessageReader = this.Input;
                this.MessageWriter = new MessageWriter(new MemoryStream(), messageSerializer);
                this.IsConnected = true;
            }

            public override Task WaitForConnection() => Task.CompletedTask;

            protected override void Shutdown()
            {
                this.IsConnected = false;
                this.Input.Messages.Writer.TryComplete();
            }
        }

        /// <summary>
        /// Tracks how many handlers are running at once and holds them until released.
        /// </summary>
        private sealed class BlockingHandler
        {
            private int active;
            private int maximumActive;
            private int completed;

            internal TaskCompletionSource<bool> FirstStarted { get; } = NewSignal();
            internal TaskCompletionSource<bool> AllCompleted { get; } = NewSignal();
            internal TaskCompletionSource<bool> Release { get; } = NewSignal();
            internal int MaximumActive => Volatile.Read(ref this.maximumActive);

            internal async Task Run()
            {
                int nowActive = Interlocked.Increment(ref this.active);
                int observed;
                while (nowActive > (observed = Volatile.Read(ref this.maximumActive)))
                {
                    Interlocked.CompareExchange(ref this.maximumActive, nowActive, observed);
                }

                this.FirstStarted.TrySetResult(true);
                await this.Release.Task;
                Interlocked.Decrement(ref this.active);
                if (Interlocked.Increment(ref this.completed) == RequestCount)
                {
                    this.AllCompleted.TrySetResult(true);
                }
            }
        }

        private static TaskCompletionSource<bool> NewSignal()
            => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static void SendParallelRequests(TestChannel channel)
        {
            for (int i = 0; i < RequestCount; i++)
            {
                channel.Input.Messages.Writer.TryWrite(Message.Request(i + 1, ParallelMethod, null));
            }
        }

        private static (TestChannel, MessageDispatcher, BlockingHandler) CreateDispatcher()
        {
            var channel = new TestChannel();
            channel.Start(MessageProtocolType.LanguageServer);
            var dispatcher = new MessageDispatcher(channel);
            var handler = new BlockingHandler();
            dispatcher.SetRequestHandler(
                RequestType<int, int>.Create(ParallelMethod),
                (_, _) => handler.Run(),
                overrideExisting: false,
                isParallelProcessingSupported: true);
            return (channel, dispatcher, handler);
        }

        [Test]
        [Timeout(20_000)]
        public async Task ParallelHandlersAreBoundedByTheConfiguredLimit()
        {
            var (channel, dispatcher, handler) = CreateDispatcher();
            dispatcher.ParallelMessageProcessingLimit = 2;
            dispatcher.ParallelMessageProcessing = true;

            try
            {
                dispatcher.Start();
                SendParallelRequests(channel);

                await handler.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await Task.Delay(500);
                Assert.That(handler.MaximumActive, Is.EqualTo(2));

                handler.Release.TrySetResult(true);
                await handler.AllCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.That(handler.MaximumActive, Is.EqualTo(2), "queued handlers all ran, still within the limit");
            }
            finally
            {
                handler.Release.TrySetResult(true);
                dispatcher.Stop();
                channel.Stop();
            }
        }

        /// <summary>
        /// The service configures the dispatcher after the host has started it.
        /// </summary>
        [Test]
        [Timeout(20_000)]
        public async Task LimitConfiguredAfterStartIsHonored()
        {
            var (channel, dispatcher, handler) = CreateDispatcher();

            try
            {
                dispatcher.Start();
                dispatcher.ParallelMessageProcessingLimit = 1;
                dispatcher.ParallelMessageProcessing = true;
                SendParallelRequests(channel);

                await handler.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                await Task.Delay(500);
                Assert.That(handler.MaximumActive, Is.EqualTo(1));

                handler.Release.TrySetResult(true);
                await handler.AllCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            finally
            {
                handler.Release.TrySetResult(true);
                dispatcher.Stop();
                channel.Stop();
            }
        }

        /// <summary>
        /// Handlers waiting for a slot must not hold up the message loop: it still has to deliver
        /// the responses and cancellations that the running handlers are waiting for.
        /// </summary>
        [Test]
        [Timeout(20_000)]
        public async Task MessageLoopKeepsDispatchingWhileParallelHandlersAreSaturated()
        {
            var (channel, dispatcher, handler) = CreateDispatcher();
            dispatcher.ParallelMessageProcessingLimit = 1;
            dispatcher.ParallelMessageProcessing = true;
            var inlineHandled = NewSignal();
            dispatcher.SetEventHandler(
                EventType<int>.Create(InlineMethod),
                (_, _) =>
                {
                    inlineHandled.TrySetResult(true);
                    return Task.CompletedTask;
                });

            try
            {
                dispatcher.Start();
                SendParallelRequests(channel);
                await handler.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

                channel.Input.Messages.Writer.TryWrite(Message.Event(InlineMethod, null));

                await inlineHandled.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.That(handler.MaximumActive, Is.EqualTo(1));
            }
            finally
            {
                handler.Release.TrySetResult(true);
                dispatcher.Stop();
                channel.Stop();
            }
        }
    }
}
