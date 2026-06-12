//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.Sts2.Contracts
{
    /// <summary>One v2 wire method (SPEC §7.2). Removing or changing an entry is a SPEC-CHANGE.</summary>
    public sealed record Sts2MethodInfo
    {
        /// <summary>Full method name including the <c>v2/</c> prefix.</summary>
        public required string Name { get; init; }

        /// <summary><c>request</c>, <c>server notification</c>, or <c>client notification</c>.</summary>
        public required string Kind { get; init; }

        /// <summary>One-line summary from the spec method table.</summary>
        public required string Summary { get; init; }

        /// <summary>Milestone in which the method becomes live.</summary>
        public required string Milestone { get; init; }

        /// <summary>True for the M1 toy surface that is removed before preview.</summary>
        public bool Toy { get; init; }
    }

    /// <summary>The v2 method registry CONTRACT.md is generated from.</summary>
    public static class Sts2Methods
    {
        /// <summary>All methods, implemented and planned, in spec table order.</summary>
        public static IReadOnlyList<Sts2MethodInfo> All { get; } =
        [
            new() { Name = "v2/initialize", Kind = "request", Summary = "Handshake, capabilities, limits, current config summary.", Milestone = "M2" },
            new() { Name = "v2/connection.open", Kind = "request", Summary = "Open a database connection; params include client-generated openId.", Milestone = "M2" },
            new() { Name = "v2/connection.cancel", Kind = "request", Summary = "Cancel an in-flight open by openId.", Milestone = "M2" },
            new() { Name = "v2/connection.close", Kind = "request", Summary = "Close a connection; an active query is canceled first.", Milestone = "M2" },
            new() { Name = "v2/query.execute", Kind = "request", Summary = "Accept a query and return queryId; completion arrives by notification.", Milestone = "M3" },
            new() { Name = "v2/query.resultSet", Kind = "server notification", Summary = "Result-set metadata.", Milestone = "M3" },
            new() { Name = "v2/query.rows", Kind = "server notification", Summary = "Forward-only row page.", Milestone = "M3" },
            new() { Name = "v2/query.message", Kind = "server notification", Summary = "Server info/error message as data.", Milestone = "M3" },
            new() { Name = "v2/query.complete", Kind = "server notification", Summary = "Exactly one terminal query completion.", Milestone = "M3" },
            new() { Name = "v2/query.ack", Kind = "client notification", Summary = "Backpressure credit or high-water mark.", Milestone = "M3" },
            new() { Name = "v2/query.cancel", Kind = "request", Summary = "Cancel query by queryId; idempotent.", Milestone = "M3" },
            new() { Name = "v2/query.dispose", Kind = "request", Summary = "Release query resources; idempotent.", Milestone = "M3" },
            new() { Name = "v2/diagnostics.ping", Kind = "request", Summary = "Echo, health summary, latest journal seq.", Milestone = "M0" },
            new() { Name = "v2/diagnostics.health", Kind = "request", Summary = "Counters, queue depths, open leases, recent errors.", Milestone = "M2" },
            new() { Name = "v2/diagnostics.state", Kind = "request", Summary = "Redacted state snapshot at current or requested seq.", Milestone = "M3" },
            new() { Name = "v2/diagnostics.exportLog", Kind = "request", Summary = "Produce a redacted export bundle.", Milestone = "M6" },
            new() { Name = "v2/diagnostics.setCapture", Kind = "request", Summary = "Change capture mode at runtime; journaled config change.", Milestone = "M4" },
            new() { Name = "v2/fatal", Kind = "server notification", Summary = "STS2 crash containment notice with redacted summary and journal path.", Milestone = "M0" },
        ];
    }
}
