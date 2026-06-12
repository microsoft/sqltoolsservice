//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;

namespace Microsoft.SqlTools.Sts2.Multiplexer
{
    /// <summary>
    /// Receives lifecycle mirroring from the multiplexer (SPEC §6.2). The legacy channel
    /// owns the JSON-RPC <c>shutdown</c> response; STS2 only observes these signals, so it
    /// can never emit a duplicate response (I14).
    /// </summary>
    public interface ISts2LifecycleSink
    {
        /// <summary>
        /// Mirror of the legacy <c>shutdown</c> request, delivered before the raw frame is
        /// forwarded to legacy. Implementations must not write any JSON-RPC response for
        /// it. The returned task completes when STS2 has flushed its journals; the
        /// multiplexer waits at most <see cref="MultiplexerOptions.ExitFlushMilliseconds"/>
        /// before forwarding anyway. The bounded wait matters here because this repo's
        /// legacy host calls <c>Environment.Exit(0)</c> from its shutdown handler and never
        /// expects an <c>exit</c> notification (DECISIONS.md RF-0011).
        /// </summary>
        Task OnShutdownAsync();

        /// <summary>
        /// Mirror of the legacy <c>exit</c> notification, delivered before the raw
        /// notification is forwarded to legacy. Same flush semantics and bound as
        /// <see cref="OnShutdownAsync"/>.
        /// </summary>
        Task OnExitAsync();
    }
}
