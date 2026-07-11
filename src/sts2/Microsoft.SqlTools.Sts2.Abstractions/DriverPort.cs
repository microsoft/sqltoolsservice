//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Sts2.Abstractions
{
    /// <summary>
    /// The driver port (SPEC §10.1). No ADO.NET types, no provider exception types:
    /// failures surface as <see cref="DbDriverException"/> with stable Sts2.* codes.
    /// Core never sees these interfaces; the Runtime effect runner owns all instances.
    /// </summary>
    public interface IDbDriver
    {
        /// <summary>Driver name as advertised by <c>v2/initialize</c> (for example <c>sqlclient</c>).</summary>
        string Name { get; }

        /// <summary>Static capabilities of this driver.</summary>
        DriverCapabilities Capabilities { get; }

        /// <summary>Opens a session. Cancellation must abort promptly and surface as <see cref="OperationCanceledException"/>.</summary>
        ValueTask<IDbSession> OpenAsync(ConnectionOpenRequest request, CancellationToken cancellationToken);
    }

    /// <summary>One open database session.</summary>
    public interface IDbSession : IAsyncDisposable
    {
        /// <summary>Server facts captured at open.</summary>
        ServerInfo Server { get; }

        /// <summary>Executes SQL, streaming events forward-only (used from M3).</summary>
        IAsyncEnumerable<ExecEvent> ExecuteAsync(QueryExecuteRequest request, CancellationToken cancellationToken);

        /// <summary>Best-effort cancel of the active query.</summary>
        ValueTask CancelAsync(string queryId, CancellationToken cancellationToken);
    }

    /// <summary>What a driver can do; reported through <c>v2/initialize</c>.</summary>
    public sealed record DriverCapabilities
    {
        /// <summary>Dialects this driver speaks (for example <c>tsql</c>, <c>sqlite</c>, <c>neutral</c>).</summary>
        public required IReadOnlyList<string> Dialects { get; init; }

        /// <summary>True for production drivers; false for test/portable ones.</summary>
        public required bool Production { get; init; }
    }

    /// <summary>Sanitized open request; the secret arrives as resolved material, never a token.</summary>
    public sealed record ConnectionOpenRequest
    {
        /// <summary>Server address (for example <c>tcp:host,1433</c> or a file path for sqlite).</summary>
        public required string Server { get; init; }

        /// <summary>Initial database/catalog; driver default when null.</summary>
        public string? Database { get; init; }

        /// <summary>Authentication material resolved from the secret side table at the edge.</summary>
        public required SecretMaterial Auth { get; init; }

        /// <summary>Open timeout in milliseconds; 0 means driver default.</summary>
        public int ConnectTimeoutMs { get; init; }

        /// <summary>Application name for connection metadata.</summary>
        public string? ApplicationName { get; init; }

        /// <summary>Provider-specific options as ordered key/value strings.</summary>
        public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
    }

    /// <summary>Resolved credential material (SPEC §10.1: passed only to the effect runner).</summary>
    public sealed record SecretMaterial
    {
        /// <summary><c>sqlLogin</c>, <c>accessToken</c>, or <c>integrated</c>.</summary>
        public required string Kind { get; init; }

        /// <summary>User name for sqlLogin, display user otherwise.</summary>
        public string? User { get; init; }

        /// <summary>Password or token; null for integrated auth. Never logged or journaled.</summary>
        public string? Secret { get; init; }
    }

    /// <summary>Server facts captured at open (SPEC §7.4 result shape).</summary>
    public sealed record ServerInfo
    {
        /// <summary>Product display name.</summary>
        public required string Product { get; init; }

        /// <summary>Server version string.</summary>
        public required string Version { get; init; }

        /// <summary>Engine edition display name when known (serverproperty('Edition')).</summary>
        public string? EngineEdition { get; init; }

        /// <summary>
        /// Numeric engine edition when known (serverproperty('EngineEdition')):
        /// 5 = Azure SQL Database, 8 = Managed Instance, … Clients use this for
        /// exact platform gating (USE vs reconnect on database switch) — the
        /// display name cannot distinguish SQL DB from MI (both "SQL Azure").
        /// </summary>
        public int? EngineEditionId { get; init; }

        /// <summary>SQL dialect the session speaks.</summary>
        public required string Dialect { get; init; }
    }

    /// <summary>Query execution request over the port (used from M3).</summary>
    public sealed record QueryExecuteRequest
    {
        /// <summary>Deterministic query id from Core.</summary>
        public required string QueryId { get; init; }

        /// <summary>SQL text (resolved at the edge; Core only ever holds digests in product capture).</summary>
        public required string Sql { get; init; }

        /// <summary>Query timeout in milliseconds; 0 means provider default.</summary>
        public int QueryTimeoutMs { get; init; }

        /// <summary>Max rows per page.</summary>
        public int PageRows { get; init; }

        /// <summary>Max bytes per page.</summary>
        public int PageBytes { get; init; }

        /// <summary>
        /// Per-cell byte bound (QO-4): lets the driver STREAM large values —
        /// bounded prefix plus honest truncation metadata — instead of
        /// materializing them for the encoder to truncate later. 0 means the
        /// pinned default. The encoder remains the authoritative bound for
        /// values the driver did not pre-bound.
        /// </summary>
        public int MaxCellBytes { get; init; }
    }

    /// <summary>
    /// Stable driver failure (SPEC §10.2): <see cref="Code"/> is one of the Sts2.* error
    /// identities; provider detail is data, never a provider exception type.
    /// </summary>
    public sealed class DbDriverException : Exception
    {
        /// <summary>Creates a classified driver failure.</summary>
        public DbDriverException(string code, string message, ServerErrorDetail? server = null, Exception? inner = null)
            : base(message, inner)
        {
            Code = code;
            Server = server;
        }

        /// <summary>Stable Sts2.* error identity.</summary>
        public string Code { get; }

        /// <summary>Server error numbers when the failure came from the engine.</summary>
        public ServerErrorDetail? Server { get; }
    }

    /// <summary>Engine error numbers (SPEC §7.6 <c>data.server</c> shape).</summary>
    public sealed record ServerErrorDetail
    {
        /// <summary>Engine error number.</summary>
        public required int Number { get; init; }

        /// <summary>Engine severity.</summary>
        public required int Severity { get; init; }

        /// <summary>Engine state.</summary>
        public required int State { get; init; }

        /// <summary>Line number when applicable.</summary>
        public int? Line { get; init; }
    }
}
