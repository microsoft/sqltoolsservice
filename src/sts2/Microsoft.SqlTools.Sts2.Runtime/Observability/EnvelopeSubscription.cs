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

        internal EnvelopeSubscription(int capacity, Action unsubscribe)
        {
            // FullMode.Wait + manual eviction gives drop-OLDEST semantics with an exact
            // drop count (DropOldest mode evicts silently and cannot be counted). The pump
            // is the only writer, so once we evict one item the subsequent write has room.
            // SingleReader is FALSE: the producer also reads (Reader.TryRead) to evict the
            // oldest, so two distinct readers can touch the channel (R034).
            channel = Channel.CreateBounded<Sts2Envelope>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false,
            });
            this.unsubscribe = unsubscribe;
        }

        /// <summary>The envelope feed, delivered in journal (seq) order.</summary>
        public ChannelReader<Sts2Envelope> Reader => channel.Reader;

        /// <summary>Envelopes evicted because this consumer did not keep up.</summary>
        public long Dropped => Interlocked.Read(ref dropped);

        /// <summary>Pushes one envelope, evicting the oldest if the buffer is full. Never blocks.</summary>
        internal bool TryPush(Sts2Envelope envelope)
        {
            if (Volatile.Read(ref disposed) != 0)
            {
                return true; // unsubscribed; not a slow-consumer drop
            }
            ChannelWriter<Sts2Envelope> writer = channel.Writer;
            if (writer.TryWrite(envelope))
            {
                return true;
            }
            // Full: evict the oldest then admit the newest (single-writer guarantees room).
            channel.Reader.TryRead(out _);
            Interlocked.Increment(ref dropped);
            writer.TryWrite(envelope);
            return false;
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
