//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Runtime.Observability;
using Microsoft.SqlTools.Sts2.UnitTests.Runtime;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Observability
{
    /// <summary>
    /// SPEC §12 event-capture framework: the journal is the write-ahead primary sink and
    /// auxiliary observers see every journaled envelope in seq order, best-effort.
    /// </summary>
    public sealed class EnvelopeSinkTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "sts2-sink-test-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        [Fact]
        public async Task AuxSinkSeesEveryJournaledEnvelopeInSeqOrder()
        {
            var capture = new CapturingEnvelopeSink();
            await using (var session = new Sts2TestSession(directory, auxSinks: [capture]))
            {
                await session.RequestAsync("v2/diagnostics.ping", """{"echo":"hi"}""");
                await session.OpenConnectionAsync();
            }

            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            // The aux sink observed exactly the journaled stream, in the journaled order.
            Assert.Equal(journal.Select(e => e.Seq), capture.Observed.Select(e => e.Seq));
            Assert.Equal(journal.Select(e => e.Kind), capture.Observed.Select(e => e.Kind));
            Assert.Equal(journal.Select(e => e.Digest), capture.Observed.Select(e => e.Digest));
        }

        [Fact]
        public async Task FaultySinkIsIsolatedAndCountedWithoutBreakingThePump()
        {
            var boom = new FaultyEnvelopeSink();
            var capture = new CapturingEnvelopeSink();
            await using (var session = new Sts2TestSession(directory, auxSinks: [boom, capture]))
            {
                OutboundRpcMessage ping = await session.RequestAsync("v2/diagnostics.ping", """{"echo":"ok"}""");
                Assert.Equal("rpc.out.result", ping.Kind); // pump unaffected by the faulting sink
                Assert.True(session.Coordinator.SinkFaultCount > 0);
            }

            // The journal is complete and a healthy sink after the faulty one still sees everything.
            List<Sts2Envelope> journal = JournalReader.ReadAll(directory).ToList();
            Assert.Equal(journal.Count, capture.Observed.Count);
            Assert.Equal(journal.Count, boom.Attempts); // every envelope was offered to the faulty sink
        }

        [Fact]
        public async Task CompositeFansOutInRegistrationOrderAndCountsFaults()
        {
            var order = new ConcurrentQueue<string>();
            var a = new DelegateEnvelopeSink((_, _) => order.Enqueue("a"));
            var faulty = new FaultyEnvelopeSink();
            var b = new DelegateEnvelopeSink((_, _) => order.Enqueue("b"));
            var composite = new CompositeEnvelopeSink([a, faulty, b]);

            await composite.OnEnvelopeAsync(Envelope(1), flush: false);

            Assert.Equal(["a", "b"], order); // faulty sink does not stop later sinks
            Assert.Equal(1, composite.FaultCount);
            Assert.Equal(3, composite.Count);
        }

        [Fact]
        public async Task BroadcastDeliversInOrderToSubscriber()
        {
            var broadcast = new BroadcastEnvelopeSink();
            using EnvelopeSubscription subscription = broadcast.Subscribe();
            Assert.Equal(1, broadcast.SubscriberCount);

            for (long seq = 1; seq <= 5; seq++)
            {
                await broadcast.OnEnvelopeAsync(Envelope(seq), flush: false);
            }

            var seqs = new List<long>();
            for (int i = 0; i < 5; i++)
            {
                Assert.True(subscription.Reader.TryRead(out Sts2Envelope? e));
                seqs.Add(e!.Seq);
            }
            Assert.Equal([1L, 2L, 3L, 4L, 5L], seqs);
            Assert.Equal(0, subscription.Dropped);
        }

        [Fact]
        public async Task BroadcastDropsOldestWithCountForSlowConsumer()
        {
            var broadcast = new BroadcastEnvelopeSink(subscriberCapacity: 4);
            using EnvelopeSubscription subscription = broadcast.Subscribe();

            // Push 10 without reading: the buffer keeps the 4 newest, drops the 6 oldest.
            for (long seq = 1; seq <= 10; seq++)
            {
                await broadcast.OnEnvelopeAsync(Envelope(seq), flush: false);
            }

            Assert.Equal(6, subscription.Dropped);
            Assert.Equal(6, broadcast.TotalDropped);

            var seqs = new List<long>();
            while (subscription.Reader.TryRead(out Sts2Envelope? e))
            {
                seqs.Add(e!.Seq);
            }
            Assert.Equal([7L, 8L, 9L, 10L], seqs); // freshest survive
        }

        [Fact]
        public async Task DisposedSubscriberStopsReceivingAndIsNotCountedAsDrop()
        {
            var broadcast = new BroadcastEnvelopeSink(subscriberCapacity: 2);
            EnvelopeSubscription subscription = broadcast.Subscribe();
            subscription.Dispose();
            Assert.Equal(0, broadcast.SubscriberCount);

            for (long seq = 1; seq <= 5; seq++)
            {
                await broadcast.OnEnvelopeAsync(Envelope(seq), flush: false);
            }
            Assert.Equal(0, broadcast.TotalDropped); // an unsubscribed consumer is not slow
        }

        private static Sts2Envelope Envelope(long seq) => new()
        {
            RunId = "test",
            Seq = seq,
            Ts = DateTimeOffset.UnixEpoch,
            Kind = EnvelopeKinds.Diagnostic,
            Type = "test.event",
            ConfigVersion = 1,
            Digest = "sha256:" + new string('0', 64),
        };

        private sealed class CapturingEnvelopeSink : IEnvelopeSink
        {
            public ConcurrentQueue<Sts2Envelope> Observed { get; } = new();

            public ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush)
            {
                Observed.Enqueue(envelope);
                return ValueTask.CompletedTask;
            }
        }

        private sealed class FaultyEnvelopeSink : IEnvelopeSink
        {
            public int Attempts { get; private set; }

            public ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush)
            {
                Attempts++;
                throw new InvalidOperationException("sink boom");
            }
        }

        private sealed class DelegateEnvelopeSink(Action<Sts2Envelope, bool> onEnvelope) : IEnvelopeSink
        {
            public ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush)
            {
                onEnvelope(envelope, flush);
                return ValueTask.CompletedTask;
            }
        }
    }
}
