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

    }
}
