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

    /// <summary>
    /// A typed float32 vector cell read natively by the driver (D-0019): explicit
    /// little-endian IEEE 754 component bytes, produced only for queries that
    /// negotiated <c>options.vectorEncoding = "binary-v1"</c>. Vectors are never
    /// truncated — a vector is complete or <see cref="DriverVectorUnavailableValue"/>.
    /// Provider CLR vector types stop at the driver boundary; this record is the
    /// provider-neutral form the encoder emits verbatim as the SPEC §7.7 vector tag.
    /// </summary>
    public sealed record DriverVectorValue
    {
        /// <summary>Component count (authoritative per cell, over column metadata).</summary>
        public required int Dimensions { get; init; }

        /// <summary>Base component type; "float32" in v1.</summary>
        public required string BaseType { get; init; }

        /// <summary>Component byte encoding; "f32le" in v1.</summary>
        public required string Encoding { get; init; }

        /// <summary>Little-endian component bytes; length == Dimensions * 4 for float32.</summary>
        public required byte[] ComponentBytes { get; init; }
    }

    /// <summary>
    /// A vector cell the driver could not transport as typed components (D-0019):
    /// honest sentinel with a stable reason, never a partial vector.
    /// </summary>
    public sealed record DriverVectorUnavailableValue
    {
        /// <summary>Component count when determinable.</summary>
        public int? Dimensions { get; init; }

        /// <summary>Base type when determinable (for example "float16").</summary>
        public string? BaseType { get; init; }

        /// <summary>"unsupportedBaseType" | "providerValueMismatch" | "decodeFailed" | "cellLimit".</summary>
        public required string Reason { get; init; }
    }

    /// <summary>
    /// Complete SQL geometry/geography interchange value (D-0020). The SqlClient
    /// provider's CLR/native representations stop at the driver boundary.
    /// </summary>
    public sealed record DriverSpatialValue
    {
        /// <summary>"geometry" or "geography".</summary>
        public required string Kind { get; init; }

        /// <summary>Authoritative SQL spatial reference identifier.</summary>
        public required int Srid { get; init; }

        /// <summary>Complete AsBinaryZM() bytes; never a prefix.</summary>
        public required byte[] Wkb { get; init; }
    }

    /// <summary>A cell-local, honest failure to produce complete spatial WKB.</summary>
    public sealed record DriverSpatialUnavailableValue
    {
        /// <summary>"geometry" or "geography".</summary>
        public required string Kind { get; init; }

        /// <summary>Stable privacy-safe failure reason.</summary>
        public required string Reason { get; init; }

        /// <summary>SRID when conversion reached that fact.</summary>
        public int? Srid { get; init; }

        /// <summary>Native provider byte count when cheaply available.</summary>
        public long? SourceBytes { get; init; }
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

        /// <summary>"geometry"/"geography" only when WKB v1 was negotiated.</summary>
        public string? SpatialKind { get; init; }

        /// <summary>"wkb-v1" only when WKB v1 was negotiated.</summary>
        public string? SpatialEncoding { get; init; }
    }
}
