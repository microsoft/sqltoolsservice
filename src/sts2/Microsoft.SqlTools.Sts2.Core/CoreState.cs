//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Immutable;

namespace Microsoft.SqlTools.Sts2.Core
{
    /// <summary>Lifecycle phase of one connection.</summary>
    public static class ConnectionPhase
    {
        /// <summary>Open effect in flight.</summary>
        public const string Opening = "opening";

        /// <summary>Session established.</summary>
        public const string Open = "open";

        /// <summary>Close effect in flight.</summary>
        public const string Closing = "closing";
    }

    /// <summary>Pure state of one connection. Handle ids are serializable tokens, never live objects.</summary>
    public sealed record ConnectionInfo
    {
        /// <summary>Server-generated connection id (<c>c-&lt;seq&gt;</c>).</summary>
        public required string ConnectionId { get; init; }

        /// <summary>Client-generated open id.</summary>
        public required string OpenId { get; init; }

        /// <summary>One of <see cref="ConnectionPhase"/>.</summary>
        public required string Phase { get; init; }

        /// <summary>Corr of the open request awaiting its terminal response (I1).</summary>
        public required string OpenCorr { get; init; }

        /// <summary>Serializable driver-session handle id once open.</summary>
        public string? HandleId { get; init; }

        /// <summary>Corr of the close request awaiting its terminal response.</summary>
        public string? CloseCorr { get; init; }

        /// <summary>True after <c>connection.cancel</c> targeted the in-flight open.</summary>
        public bool CancelRequested { get; init; }

        /// <summary>The single active query on this connection (SPEC §7.9: one at a time).</summary>
        public string? ActiveQueryId { get; init; }

        /// <summary>True when a close waits for the active query to reach a terminal state.</summary>
        public bool CloseAfterQuery { get; init; }
    }

    /// <summary>Lifecycle phase of one query.</summary>
    public static class QueryPhase
    {
        /// <summary>Streaming or about to stream.</summary>
        public const string Running = "running";

        /// <summary>Cancel requested; awaiting the terminal driver event.</summary>
        public const string CancelRequested = "cancelRequested";

        /// <summary>Dispose requested on an active query; awaiting the runner's pump-stopped confirmation (D-0011).</summary>
        public const string Disposing = "disposing";

        /// <summary>Terminal: query.complete sent exactly once (I2).</summary>
        public const string Completed = "completed";

        /// <summary>Disposed: resources released, further output suppressed (I3).</summary>
        public const string Disposed = "disposed";
    }

    /// <summary>Pure state of one query. Never contains row cells (SPEC §9.2).</summary>
    public sealed record QueryInfo
    {
        /// <summary>Server-generated query id (<c>q-&lt;seq&gt;</c>).</summary>
        public required string QueryId { get; init; }

        /// <summary>Owning connection.</summary>
        public required string ConnectionId { get; init; }

        /// <summary>One of <see cref="QueryPhase"/>.</summary>
        public required string Phase { get; init; }

        /// <summary>Rows pages sent to the client so far.</summary>
        public required int PagesSent { get; init; }

        /// <summary>Rows pages acked by the client (count, monotonic).</summary>
        public required int PagesAcked { get; init; }

        /// <summary>Credit granted to the effect runner not yet consumed by a page.</summary>
        public required int CreditOutstanding { get; init; }

        /// <summary>True once <c>query.complete</c> was emitted (I2/I3 guard).</summary>
        public required bool CompleteSent { get; init; }
    }

    /// <summary>A driver advertised by <c>v2/initialize</c>.</summary>
    public sealed record DriverDescriptor
    {
        /// <summary>Driver name (<c>sqlclient</c>, <c>sqlite</c>, <c>fake</c>).</summary>
        public required string Name { get; init; }

        /// <summary>Dialects the driver speaks.</summary>
        public required ImmutableArray<string> Dialects { get; init; }

        /// <summary>True for production drivers.</summary>
        public required bool Production { get; init; }
    }

    /// <summary>
    /// Immutable Core state. Connection machine is live (M2); query machine lands in M3;
    /// the toy machine remains until M3 as spine scaffolding. Never contains row cells,
    /// secrets, or live handles (SPEC §9.2). All maps are sorted for deterministic output
    /// ordering (SPEC §9.3.5).
    /// </summary>
    public sealed record CoreState
    {
        /// <summary>
        /// The one and only initial state. Session config (service version, drivers)
        /// enters through a journaled <c>session.start</c> control envelope, never
        /// through construction — otherwise replay would start from a different state
        /// than the live run did.
        /// </summary>
        public static CoreState Initial { get; } = new()
        {
            LastSeq = 0,
            ShuttingDown = false,
            Initialized = false,
            ServiceVersion = "0.0.0.0",
            MaxConnections = Contracts.Sts2Defaults.MaxConnections,
            RowCapture = "full",
            SqlCapture = "text",
            // Permissive ceiling by default (dev/test composition). The product composition
            // pins a deny policy (maxRow=digest, maxSql=digest) via session.start (D-0012).
            MaxRowCapture = "full",
            MaxSqlCapture = "text",
            ConfigVersion = 1,
            Drivers = ImmutableArray<DriverDescriptor>.Empty,
            Connections = ImmutableSortedDictionary<string, ConnectionInfo>.Empty,
            OpenIdToConnectionId = ImmutableSortedDictionary<string, string>.Empty,
            Queries = ImmutableSortedDictionary<string, QueryInfo>.Empty,
        };

        /// <summary>Seq of the last envelope this state reflects.</summary>
        public required long LastSeq { get; init; }

        /// <summary>True after a lifecycle shutdown/exit signal.</summary>
        public required bool ShuttingDown { get; init; }

        /// <summary>True after the first <c>v2/initialize</c>; repeated calls do not reset state.</summary>
        public required bool Initialized { get; init; }

        /// <summary>Service version reported by initialize/ping.</summary>
        public required string ServiceVersion { get; init; }

        /// <summary>Connection limit; configurable via journaled session.start limits.</summary>
        public required int MaxConnections { get; init; }

        /// <summary>Row capture mode (<c>full</c> | <c>digest</c>); seeded by session.start, changed by setCapture (SPEC §8.4).</summary>
        public required string RowCapture { get; init; }

        /// <summary>SQL capture mode (<c>text</c> | <c>digest</c>); seeded by session.start, changed by setCapture (SPEC §8.4).</summary>
        public required string SqlCapture { get; init; }

        /// <summary>Host policy ceiling for row capture (D-0012): setCapture may not exceed this. Product denies <c>full</c>.</summary>
        public required string MaxRowCapture { get; init; }

        /// <summary>Host policy ceiling for SQL capture (D-0012): setCapture may not exceed this. Product denies <c>text</c>.</summary>
        public required string MaxSqlCapture { get; init; }

        /// <summary>Monotonic config snapshot version; bumped on each capture change (SPEC §8.4, I15).</summary>
        public required int ConfigVersion { get; init; }

        /// <summary>Drivers available in this composition.</summary>
        public required ImmutableArray<DriverDescriptor> Drivers { get; init; }

        /// <summary>Connections by connection id.</summary>
        public required ImmutableSortedDictionary<string, ConnectionInfo> Connections { get; init; }

        /// <summary>In-flight and open openId index for cancel routing and duplicate detection.</summary>
        public required ImmutableSortedDictionary<string, string> OpenIdToConnectionId { get; init; }

        /// <summary>Queries by query id, including terminal ones until disposed.</summary>
        public required ImmutableSortedDictionary<string, QueryInfo> Queries { get; init; }
    }
}
