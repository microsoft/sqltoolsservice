//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;

namespace Microsoft.SqlTools.Sts2.Runtime.Observability
{
    /// <summary>
    /// The universal observation hook (SPEC §12). Every envelope the coordinator journals
    /// is also handed, in strict <see cref="Sts2Envelope.Seq"/> order, to each registered
    /// sink. This is the one seam new observers attach to — the journal, metrics, the live
    /// diagnostic tail, and test capture are all sinks over the same stream.
    /// <para>
    /// The journal is the privileged write-ahead sink: it is appended and awaited BEFORE
    /// Core dispatches the envelope (§8.3). Auxiliary sinks observe AFTER journaling and
    /// are best-effort — they MUST NOT block (complete synchronously or near-synchronously)
    /// and their failures are isolated by the coordinator so a slow or faulty observer can
    /// never stall the pump or violate write-ahead.
    /// </para>
    /// </summary>
    public interface IEnvelopeSink
    {
        /// <summary>
        /// Observes one journaled envelope. <paramref name="flush"/> mirrors the journal's
        /// flush decision (terminal responses, completion, fatal diagnostics, lifecycle).
        /// Implementations must not throw; the coordinator still guards the call.
        /// </summary>
        ValueTask OnEnvelopeAsync(Sts2Envelope envelope, bool flush);
    }
}
