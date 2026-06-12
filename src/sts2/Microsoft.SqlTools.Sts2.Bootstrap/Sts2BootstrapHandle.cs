//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Hosting;
using Microsoft.SqlTools.Sts2.Multiplexer;

namespace Microsoft.SqlTools.Sts2.Bootstrap
{
    /// <summary>
    /// Owns the STS2 runtime pieces for one process run (SPEC §5.1). When disabled, the
    /// legacy stream properties are null so <c>ServiceHost.Initialize(null, null)</c>
    /// keeps its existing console-stream behavior untouched.
    /// </summary>
    public sealed class Sts2BootstrapHandle : IAsyncDisposable
    {
        private readonly StdioMultiplexer? multiplexer;
        private readonly Sts2RpcHost? rpcHost;
        private readonly StreamWriter? diagnosticsLog;

        internal static Sts2BootstrapHandle Disabled { get; } = new(null, null, null, null, null);

        internal Sts2BootstrapHandle(
            Stream? legacyInputStream,
            Stream? legacyOutputStream,
            StdioMultiplexer? multiplexer,
            Sts2RpcHost? rpcHost,
            StreamWriter? diagnosticsLog)
        {
            LegacyInputStream = legacyInputStream;
            LegacyOutputStream = legacyOutputStream;
            this.multiplexer = multiplexer;
            this.rpcHost = rpcHost;
            this.diagnosticsLog = diagnosticsLog;
        }

        /// <summary>True when STS2 was activated for this run.</summary>
        public bool IsEnabled => multiplexer is not null;

        /// <summary>Virtual stream legacy reads from, or null when STS2 is disabled.</summary>
        public Stream? LegacyInputStream { get; }

        /// <summary>Virtual stream legacy writes to, or null when STS2 is disabled.</summary>
        public Stream? LegacyOutputStream { get; }

        /// <summary>
        /// Flushes and closes STS2 without blocking legacy shutdown indefinitely (SPEC §5.1).
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (rpcHost is not null)
            {
                await rpcHost.DisposeAsync().ConfigureAwait(false);
            }
            if (multiplexer is not null)
            {
                await multiplexer.DisposeAsync().ConfigureAwait(false);
            }
            if (diagnosticsLog is not null)
            {
                await diagnosticsLog.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
