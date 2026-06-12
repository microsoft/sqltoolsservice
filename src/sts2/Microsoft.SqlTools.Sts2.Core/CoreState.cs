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
            Drivers = ImmutableArray<DriverDescriptor>.Empty,
            Connections = ImmutableSortedDictionary<string, ConnectionInfo>.Empty,
            OpenIdToConnectionId = ImmutableSortedDictionary<string, string>.Empty,
            ToyCounter = 0,
            PendingToyEffects = ImmutableSortedDictionary<string, string>.Empty,
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

        /// <summary>Drivers available in this composition.</summary>
        public required ImmutableArray<DriverDescriptor> Drivers { get; init; }

        /// <summary>Connections by connection id.</summary>
        public required ImmutableSortedDictionary<string, ConnectionInfo> Connections { get; init; }

        /// <summary>In-flight and open openId index for cancel routing and duplicate detection.</summary>
        public required ImmutableSortedDictionary<string, string> OpenIdToConnectionId { get; init; }

        /// <summary>Toy: number of <c>v2/toy.echo</c> requests handled (removed in M3).</summary>
        public required int ToyCounter { get; init; }

        /// <summary>Toy: effect id -> originating RPC corr (removed in M3).</summary>
        public required ImmutableSortedDictionary<string, string> PendingToyEffects { get; init; }
    }
}
