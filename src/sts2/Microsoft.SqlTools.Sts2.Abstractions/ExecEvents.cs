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

    /// <summary>Execution finished.</summary>
    public sealed record ExecCompleted(IReadOnlyList<long> RowsAffected) : ExecEvent;

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
