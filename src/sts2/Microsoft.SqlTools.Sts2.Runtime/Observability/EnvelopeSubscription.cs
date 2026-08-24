//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Channels;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Observability
{
    /// <summary>
    /// One live consumer of the broadcast envelope stream. Read <see cref="Reader"/> to pull
    /// envelopes in seq order; <see cref="Dropped"/> reports how many were evicted because
    /// this consumer fell behind. Dispose to unregister and complete the reader.
    /// </summary>
    public sealed class EnvelopeSubscription : IDisposable
    {
        private readonly Channel<Sts2Envelope> channel;
        private readonly Action unsubscribe;
        private long dropped;
        private int disposed;

        internal EnvelopeSubscription(int capacity, Action unsubscribe, Action onDropped)
        {
            channel = Channel.CreateBounded<Sts2Envelope>(
                new BoundedChannelOptions(capacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleWriter = true,
                    SingleReader = true,
                },
                _ =>
                {
                    Interlocked.Increment(ref dropped);
                    onDropped();
                });
            this.unsubscribe = unsubscribe;
        }

        /// <summary>The envelope feed, delivered in journal (seq) order.</summary>
        public ChannelReader<Sts2Envelope> Reader => channel.Reader;

        /// <summary>Envelopes evicted because this consumer did not keep up.</summary>
        public long Dropped => Interlocked.Read(ref dropped);

        /// <summary>Pushes one envelope, evicting the oldest if the buffer is full. Never blocks.</summary>
        internal void TryPush(Sts2Envelope envelope)
        {
            if (Volatile.Read(ref disposed) != 0)
            {
                return; // unsubscribed; not a slow-consumer drop
            }
            channel.Writer.TryWrite(envelope);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }
            unsubscribe();
            channel.Writer.TryComplete();
        }
    }
}
