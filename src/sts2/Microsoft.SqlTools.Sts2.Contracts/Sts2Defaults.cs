//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Sts2.Contracts
{
    /// <summary>Pinned defaults (SPEC §11.2). Changing a value is a SPEC-CHANGE / deviation-log entry.</summary>
    public static class Sts2Defaults
    {
        /// <summary>Max rows per page (<c>sts2.results.pageRows</c>).</summary>
        public const int PageRows = 1000;

        /// <summary>Max bytes per page (<c>sts2.results.pageBytes</c>).</summary>
        public const int PageBytes = 262144;

        /// <summary>Unacked pages per query (<c>sts2.results.windowPages</c>).</summary>
        public const int WindowPages = 4;

        /// <summary>Cell truncation threshold (<c>sts2.results.maxCellBytes</c>).</summary>
        public const int MaxCellBytes = 1048576;

        /// <summary>Retained prefix for truncated cells (<c>sts2.results.truncatedPrefixBytes</c>).</summary>
        public const int TruncatedPrefixBytes = 65536;

        /// <summary>Max transport frame (<c>sts2.transport.maxFrameBytes</c>).</summary>
        public const int MaxFrameBytes = 67108864;

        /// <summary>Journal segment rotation size (<c>sts2.journal.segmentBytes</c>).</summary>
        public const int JournalSegmentBytes = 67108864;

        /// <summary>Journal flush interval bound (<c>sts2.journal.flushIntervalMs</c>).</summary>
        public const int JournalFlushIntervalMs = 250;

        /// <summary>Default query timeout; 0 means provider default (<c>sts2.query.defaultTimeoutMs</c>).</summary>
        public const int QueryDefaultTimeoutMs = 0;

        /// <summary>Default connect timeout (<c>sts2.connection.connectTimeoutMs</c>).</summary>
        public const int ConnectTimeoutMs = 15000;

        /// <summary>Max concurrent connections; beyond fails with Sts2.Busy (<c>sts2.runtime.maxConnections</c>).</summary>
        public const int MaxConnections = 64;

        /// <summary>Bounded close after cancellation (<c>sts2.runtime.closeTimeoutMs</c>).</summary>
        public const int CloseTimeoutMs = 5000;

        /// <summary>Bounded journal flush before exit/shutdown forwards (<c>sts2.runtime.exitFlushMs</c>).</summary>
        public const int ExitFlushMs = 500;
    }

    /// <summary>
    /// Stable numeric JSON-RPC error codes per Sts2 error identity (SPEC §7.6: numeric
    /// on the wire, string identity in <c>error.data.code</c>).
    /// </summary>
    public static class Sts2JsonRpcCodes
    {
        /// <summary>Maps an <see cref="Sts2ErrorCodes"/> identity to its numeric JSON-RPC code.</summary>
        public static int For(string dataCode) => dataCode switch
        {
            Sts2ErrorCodes.ConnectionFailedAuth => -32040,
            Sts2ErrorCodes.ConnectionFailedNetwork => -32041,
            Sts2ErrorCodes.ConnectionFailedTimeout => -32042,
            Sts2ErrorCodes.QueryFailedServer => -32050,
            Sts2ErrorCodes.QueryFailedTransport => -32051,
            Sts2ErrorCodes.Canceled => -32060,
            Sts2ErrorCodes.Busy => -32061,
            Sts2ErrorCodes.NotFound => -32062,
            Sts2ErrorCodes.Unavailable => -32063,
            Sts2ErrorCodes.InvalidRequest => -32600,
            _ => -32603, // Sts2.Internal and anything unmapped
        };
    }
}
