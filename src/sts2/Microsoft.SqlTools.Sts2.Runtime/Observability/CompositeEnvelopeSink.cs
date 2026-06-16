//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Observability
{
    /// <summary>
    /// Fans one envelope out to an ordered list of auxiliary sinks with fault isolation.
    /// Sinks are invoked in registration order, in strict seq order, after the envelope
    /// has been journaled. A sink that throws is counted (see <see cref="FaultCount"/>) and
    /// skipped — it never reorders the stream, stalls the pump, or breaks write-ahead.
    /// </summary>
    public sealed class CompositeEnvelopeSink : IEnvelopeSink
    {
        private readonly IReadOnlyList<IEnvelopeSink> sinks;
        private readonly Action<IEnvelopeSink, Exception>? onFault;
        private long faultCount;

        /// <summary>Creates a composite over <paramref name="sinks"/> (registration order is observation order).</summary>
        public CompositeEnvelopeSink(IReadOnlyList<IEnvelopeSink> sinks, Action<IEnvelopeSink, Exception>? onFault = null)
        {
            this.sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
            this.onFault = onFault;
        }

        /// <summary>Number of registered sinks.</summary>
        public int Count => sinks.Count;

        /// <summary>Total auxiliary-sink faults swallowed since construction.</summary>
        public long FaultCount => Interlocked.Read(ref faultCount);

        /// <inheritdoc/>
        public async ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush)
        {
            // Hot path: avoid the await machinery when there is nothing to fan out.
            for (int i = 0; i < sinks.Count; i++)
            {
                IEnvelopeSink sink = sinks[i];
                try
                {
                    await sink.OnEnvelopeAsync(envelope, flush).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref faultCount);
                    onFault?.Invoke(sink, ex);
                }
            }
        }
    }
}
