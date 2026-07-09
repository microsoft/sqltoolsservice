//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.Sts2.Abstractions
{
    /// <summary>One event observed while executing a query (SPEC §10.1). Used from M3.</summary>
    public abstract record ExecEvent;

    /// <summary>Execution accepted by the engine.</summary>
    public sealed record ExecStarted(string QueryId) : ExecEvent;

    /// <summary>A result set began.</summary>
    public sealed record ResultSetStarted(int ResultSetId, IReadOnlyList<ColumnInfo> Columns) : ExecEvent;

    /// <summary>One forward-only page of rows; cells are wire-encoded values.</summary>
    public sealed record RowsPage(int ResultSetId, int PageSeq, long RowOffset, IReadOnlyList<IReadOnlyList<object?>> Cells) : ExecEvent;

    /// <summary>An engine info or error message, as data.</summary>
    public sealed record ServerMessage(string MessageClass, int Number, int Severity, string Text, int? Line) : ExecEvent;

    /// <summary>A result set finished.</summary>
    public sealed record ResultSetCompleted(int ResultSetId, long RowCount) : ExecEvent;

    /// <summary>
    /// Execution finished. Database is the connection's CURRENT database when
    /// the driver can observe it (SqlClient tracks ENVCHANGE) — the client's
    /// source of truth for USE statements executed in scripts.
    /// </summary>
    public sealed record ExecCompleted(IReadOnlyList<long> RowsAffected, string? Database = null) : ExecEvent;

    /// <summary>
    /// A large cell the DRIVER pre-bounded by streaming (QO-4): the retained
    /// prefix plus honest metadata, produced without ever materializing the
    /// full value. Exactly one of <see cref="PrefixText"/>/<see cref="PrefixBytes"/>
    /// is set per <see cref="Kind"/>. <see cref="TotalBytes"/> counts the full
    /// value's bytes (UTF-8 for text) and <see cref="DigestHex"/> is the
    /// sha256 of the full value, both computed while streaming — the encoder
    /// emits the SPEC §7.7 truncation wrapper from these fields verbatim.
    /// </summary>
    public sealed record DriverTruncatedValue
    {
        /// <summary>"string" or "binary" — how the prefix decodes.</summary>
        public required string Kind { get; init; }

        /// <summary>Retained text prefix (Kind == "string").</summary>
        public string? PrefixText { get; init; }

        /// <summary>Retained binary prefix (Kind == "binary").</summary>
        public byte[]? PrefixBytes { get; init; }

        /// <summary>Full value byte count (UTF-8 for text), streamed.</summary>
        public required long TotalBytes { get; init; }

        /// <summary>sha256 hex of the full value bytes, streamed.</summary>
        public required string DigestHex { get; init; }
    }

    /// <summary>Column metadata: engine type names verbatim plus normalized fields (SPEC §7.7).</summary>
    public sealed record ColumnInfo
    {
        /// <summary>Column name.</summary>
        public required string Name { get; init; }

        /// <summary>Engine type name verbatim.</summary>
        public required string EngineType { get; init; }

        /// <summary>True when the column allows nulls; null when unknown.</summary>
        public bool? Nullable { get; init; }

        /// <summary>Numeric precision when known.</summary>
        public int? Precision { get; init; }

        /// <summary>Numeric scale when known.</summary>
        public int? Scale { get; init; }

        /// <summary>Max length when known.</summary>
        public int? Length { get; init; }

        /// <summary>Collation when known.</summary>
        public string? Collation { get; init; }
    }
}
