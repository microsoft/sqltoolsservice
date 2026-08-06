//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Observability
{
    /// <summary>
    /// Decouples a (possibly slow or untrusted) <see cref="IEnvelopeSink"/> from the
    /// coordinator pump (SPEC §12, R003). The pump's publish is a non-blocking
    /// <c>TryWrite</c> into a bounded mailbox; a background worker drains the mailbox and
    /// invokes the inner sink. A sink that blocks, hangs, or throws can therefore never
    /// stall the pump or break write-ahead — it only falls behind and drops the oldest
    /// envelopes (counted). This is the wrapper the session puts around third-party sinks;
    /// the built-in metrics and live-tail sinks are already non-blocking and run inline.
    /// </summary>
    public sealed class MailboxEnvelopeSink : IEnvelopeSink, IAsyncDisposable
    {
        private readonly IEnvelopeSink inner;
        private readonly Channel<(Sts2Envelope Envelope, bool Flush)> mailbox;
        private readonly Task worker;
        private long dropped;
        private long faults;

        /// <summary>Wraps <paramref name="inner"/> behind a bounded mailbox of <paramref name="capacity"/>.</summary>
        public MailboxEnvelopeSink(IEnvelopeSink inner, int capacity = 4096)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
            mailbox = Channel.CreateBounded<(Sts2Envelope, bool)>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false, // the writer also reads to evict the oldest on overflow
            });
            worker = Task.Run(DrainAsync);
        }

        /// <summary>Envelopes dropped because the inner sink fell behind.</summary>
        public long Dropped => Interlocked.Read(ref dropped);

        /// <summary>Inner-sink invocations that threw (swallowed, never reach the pump).</summary>
        public long FaultCount => Interlocked.Read(ref faults);

        /// <inheritdoc/>
        public ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush)
        {
            // Non-blocking: the pump never awaits the inner sink. On overflow, drop the oldest
            // to keep the freshest (a live observer wants recency) and count the drop.
            if (!mailbox.Writer.TryWrite((envelope, flush)))
            {
                mailbox.Reader.TryRead(out _);
                Interlocked.Increment(ref dropped);
                mailbox.Writer.TryWrite((envelope, flush));
            }
            return ValueTask.CompletedTask;
        }

        private async Task DrainAsync()
        {
            await foreach ((Sts2Envelope envelope, bool flush) in mailbox.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                try
                {
                    await inner.OnEnvelopeAsync(envelope, flush).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref faults);
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            mailbox.Writer.TryComplete();
            try
            {
                await worker.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The worker swallows inner-sink faults; nothing should surface here.
            }
        }
    }
}
