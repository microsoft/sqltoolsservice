//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.Sts2.Testing
{
    /// <summary>One scripted execution event for FakeDriver (SPEC §10.4).</summary>
    public sealed record FakeQueryStep
    {
        /// <summary>
        /// <c>resultSet</c>, <c>rows</c>, <c>message</c>, <c>resultSetDone</c>,
        /// <c>completed</c>, <c>error</c>, <c>hang</c> (released by cancel), or
        /// <c>sever</c> (transport death mid-stream).
        /// </summary>
        public required string Type { get; init; }

        /// <summary>Result set ordinal for resultSet/rows/resultSetDone.</summary>
        public int ResultSetId { get; init; }

        /// <summary>Column count for resultSet steps.</summary>
        public int Columns { get; init; } = 2;

        /// <summary>Row count for rows steps (one page per step; pageSeq assigned by the driver).</summary>
        public int Rows { get; init; }

        /// <summary>True to fabricate typed-wrapper edge values instead of natives.</summary>
        public bool EdgeValues { get; init; }

        /// <summary>Message text for message steps.</summary>
        public string? Text { get; init; }

        /// <summary>Engine number for message/error steps.</summary>
        public int Number { get; init; }

        /// <summary>Severity for message/error steps.</summary>
        public int Severity { get; init; }

        /// <summary>Engine line number for message steps; null when the engine gave none.</summary>
        public int? Line { get; init; }

        /// <summary>Stable Sts2.* code for error steps.</summary>
        public string? ErrorCode { get; init; }

        /// <summary>Total row count for resultSetDone steps.</summary>
        public long RowCount { get; init; }

        /// <summary>Rows affected for completed steps.</summary>
        public long RowsAffected { get; init; }

        /// <summary>Delay before this step materializes.</summary>
        public int DelayMs { get; init; }
    }

    /// <summary>A full scripted execution: the events one ExecuteAsync call observes.</summary>
    public sealed record FakeQueryScript
    {
        /// <summary>Steps in order.</summary>
        public required IReadOnlyList<FakeQueryStep> Steps { get; init; }

        /// <summary>The default script: one 2-column result set, one 3-row page, completed.</summary>
        public static FakeQueryScript Default { get; } = new()
        {
            Steps =
            [
                new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 2 },
                new FakeQueryStep { Type = "rows", ResultSetId = 0, Rows = 3 },
                new FakeQueryStep { Type = "resultSetDone", ResultSetId = 0, RowCount = 3 },
                new FakeQueryStep { Type = "completed", RowsAffected = 3 },
            ],
        };
    }
}
