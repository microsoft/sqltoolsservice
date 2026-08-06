//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Observability
{
    /// <summary>
    /// In-process live tail (SPEC §12): pushes every observed envelope to each active
    /// subscriber's bounded buffer. This is the diagnostic viewer's primary feed and the
    /// metrics emitter's hook. A subscriber that falls behind never stalls the pump — its
    /// buffer drops the OLDEST envelope to admit the newest (a live tail wants freshness),
    /// and the eviction is counted per subscriber so the consumer can detect the gap and
    /// re-sync from the journal. Writes never block (write-ahead and pump liveness hold).
    /// </summary>
    public sealed class BroadcastEnvelopeSink : IEnvelopeSink
    {
        private readonly int capacity;
        private readonly ConcurrentDictionary<long, EnvelopeSubscription> subscribers = new();
        private long nextId;
        private long totalDropped;

        /// <summary>Creates the broadcast sink. <paramref name="subscriberCapacity"/> bounds each subscriber's buffer.</summary>
        public BroadcastEnvelopeSink(int subscriberCapacity = 4096)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(subscriberCapacity, 1);
            capacity = subscriberCapacity;
        }

        /// <summary>Number of active subscribers.</summary>
        public int SubscriberCount => subscribers.Count;

        /// <summary>Total envelopes dropped across all subscribers (slow-consumer pressure).</summary>
        public long TotalDropped => Interlocked.Read(ref totalDropped);

        /// <summary>
        /// Registers a live subscriber. Read <see cref="EnvelopeSubscription.Reader"/> to
        /// consume envelopes; dispose the subscription to unregister.
        /// </summary>
        public EnvelopeSubscription Subscribe()
        {
            long id = Interlocked.Increment(ref nextId);
            var subscription = new EnvelopeSubscription(capacity, () => subscribers.TryRemove(id, out _));
            subscribers[id] = subscription;
            return subscription;
        }

        /// <inheritdoc/>
        public ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush)
        {
            foreach (KeyValuePair<long, EnvelopeSubscription> entry in subscribers)
            {
                if (!entry.Value.TryPush(envelope))
                {
                    Interlocked.Increment(ref totalDropped);
                }
            }
            return ValueTask.CompletedTask;
        }
    }
}
