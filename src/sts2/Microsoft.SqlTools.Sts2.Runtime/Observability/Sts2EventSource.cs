//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.SqlTools.Sts2.Runtime.Observability
{
    /// <summary>
    /// The out-of-process metrics surface (SPEC §12.3). A process-wide
    /// <see cref="EventSource"/> exposing polling counters over the aggregate envelope
    /// stream, so <c>dotnet-counters monitor Microsoft-SqlTools-Sts2</c> (or any
    /// <see cref="EventListener"/>) sees live rates without attaching to the journal.
    /// Counters are fed by every <see cref="MetricsEnvelopeSink"/>; the per-session sink
    /// keeps the readable, replay-friendly tallies that health and <c>metric</c> envelopes
    /// report. BCL-only: <see cref="PollingCounter"/> ships in the shared runtime.
    /// </summary>
    [EventSource(Name = "Microsoft-SqlTools-Sts2")]
    public sealed class Sts2EventSource : EventSource
    {
        /// <summary>The process-wide instance.</summary>
        public static readonly Sts2EventSource Log = new();

        private static long envelopeTotal;
        private static long errorTotal;
        private static long sinkFaultTotal;

        private PollingCounter? envelopes;
        private PollingCounter? errors;
        private PollingCounter? sinkFaults;

        private Sts2EventSource()
        {
        }

        /// <summary>Records one observed envelope.</summary>
        public void EnvelopeObserved() => Interlocked.Increment(ref envelopeTotal);

        /// <summary>Records one outbound rpc.out.error.</summary>
        public void ErrorObserved() => Interlocked.Increment(ref errorTotal);

        /// <summary>Records one auxiliary-sink fault.</summary>
        public void SinkFaultObserved() => Interlocked.Increment(ref sinkFaultTotal);

        /// <inheritdoc/>
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command != EventCommand.Enable)
            {
                return;
            }
            envelopes ??= new PollingCounter("envelopes-total", this, () => Interlocked.Read(ref envelopeTotal))
            {
                DisplayName = "Envelopes Observed",
            };
            errors ??= new PollingCounter("rpc-errors-total", this, () => Interlocked.Read(ref errorTotal))
            {
                DisplayName = "RPC Errors",
            };
            sinkFaults ??= new PollingCounter("sink-faults-total", this, () => Interlocked.Read(ref sinkFaultTotal))
            {
                DisplayName = "Aux Sink Faults",
            };
        }
    }
}
