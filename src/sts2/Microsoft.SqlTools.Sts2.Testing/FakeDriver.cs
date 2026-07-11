//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Testing
{
    /// <summary>How one FakeDriver open attempt behaves (SPEC §10.4).</summary>
    public sealed record FakeOpenBehavior
    {
        /// <summary><c>ok</c>, <c>authFail</c>, <c>networkFail</c>, <c>timeout</c>, or <c>hang</c> (released only by cancellation).</summary>
        public string Outcome { get; init; } = "ok";

        /// <summary>Delay before the outcome materializes.</summary>
        public int DelayMs { get; init; }
    }

    /// <summary>
    /// Deterministic scripted driver (SPEC §10.4). M2 scope: open outcomes, hang points
    /// released by cancel, and session lease tracking. Query scripting lands in M3.
    /// </summary>
    public sealed class FakeDriver : IDbDriver
    {
        private readonly ConcurrentQueue<FakeOpenBehavior> openBehaviors = new();
        private readonly ConcurrentQueue<FakeQueryScript> queryScripts = new();
        private int openSessions;

        /// <inheritdoc/>
        public string Name => "fake";

        /// <inheritdoc/>
        public DriverCapabilities Capabilities { get; } = new() { Dialects = ["neutral", "tsql"], Production = false };

        /// <summary>Live session count; I8 asserts this returns to zero by run end.</summary>
        public int OpenSessionCount => Volatile.Read(ref openSessions);

        /// <summary>The most recent ExecuteAsync request — lets tests assert option pass-through (QO-3).</summary>
        public QueryExecuteRequest? LastExecuteRequest { get; private set; }

        /// <summary>The most recent open request — lets tests assert auth material selection at the runtime edge.</summary>
        public ConnectionOpenRequest? LastOpenRequest { get; private set; }

        /// <summary>Server facts every successful open reports.</summary>
        public ServerInfo ServerInfo { get; init; } = new()
        {
            Product = "Fake 1.0",
            Version = "1.0.0",
            EngineEdition = "Test",
            Dialect = "neutral",
        };

        /// <summary>Queues the behavior for the next open attempt; defaults to immediate <c>ok</c> when empty.</summary>
        public FakeDriver EnqueueOpen(FakeOpenBehavior behavior)
        {
            openBehaviors.Enqueue(behavior);
            return this;
        }

        /// <summary>Queues the script for the next ExecuteAsync call; defaults to <see cref="FakeQueryScript.Default"/>.</summary>
        public FakeDriver EnqueueQuery(FakeQueryScript script)
        {
            queryScripts.Enqueue(script);
            return this;
        }

        /// <inheritdoc/>
        public async ValueTask<IDbSession> OpenAsync(ConnectionOpenRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            LastOpenRequest = request;
            FakeOpenBehavior behavior = openBehaviors.TryDequeue(out FakeOpenBehavior? scripted) ? scripted : new FakeOpenBehavior();

            if (behavior.DelayMs > 0)
            {
                await Task.Delay(behavior.DelayMs, cancellationToken).ConfigureAwait(false);
            }

            switch (behavior.Outcome)
            {
                case "ok":
                    Interlocked.Increment(ref openSessions);
                    return new FakeSession(this);

                case "authFail":
                    throw new DbDriverException(Sts2ErrorCodes.ConnectionFailedAuth, "Login failed.",
                        new ServerErrorDetail { Number = 18456, Severity = 14, State = 1 });

                case "networkFail":
                    throw new DbDriverException(Sts2ErrorCodes.ConnectionFailedNetwork, "Network path not found.");

                case "timeout":
                    throw new DbDriverException(Sts2ErrorCodes.ConnectionFailedTimeout, "Connection attempt timed out.");

                case "hang":
                    // Released only by cancellation: the open-cancel scenarios depend on this.
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                    throw new InvalidOperationException("unreachable");

                default:
                    throw new DbDriverException(Sts2ErrorCodes.Internal, "Unknown scripted outcome: " + behavior.Outcome);
            }
        }

        private void SessionClosed() => Interlocked.Decrement(ref openSessions);

        private sealed class FakeSession : IDbSession
        {
            private readonly FakeDriver owner;
            private readonly CancellationTokenSource queryCancel = new();
            private int disposed;

            internal FakeSession(FakeDriver owner)
            {
                this.owner = owner;
            }

            public ServerInfo Server => owner.ServerInfo;

            public async IAsyncEnumerable<ExecEvent> ExecuteAsync(QueryExecuteRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                owner.LastExecuteRequest = request;
                FakeQueryScript script = owner.queryScripts.TryDequeue(out FakeQueryScript? scripted)
                    ? scripted
                    : FakeQueryScript.Default;
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, queryCancel.Token);

                yield return new ExecStarted(request.QueryId);
                var pageSeqByResultSet = new Dictionary<int, int>();
                var rowOffsetByResultSet = new Dictionary<int, long>();

                foreach (FakeQueryStep step in script.Steps)
                {
                    if (step.DelayMs > 0)
                    {
                        await Task.Delay(step.DelayMs, linked.Token).ConfigureAwait(false);
                    }
                    linked.Token.ThrowIfCancellationRequested();

                    switch (step.Type)
                    {
                        case "resultSet":
                            yield return new ResultSetStarted(step.ResultSetId, FabricateColumns(step.Columns, step.EdgeValues));
                            break;

                        case "rows":
                        {
                            int pageSeq = pageSeqByResultSet.TryGetValue(step.ResultSetId, out int p) ? p : 0;
                            long rowOffset = rowOffsetByResultSet.TryGetValue(step.ResultSetId, out long o) ? o : 0;
                            yield return new RowsPage(step.ResultSetId, pageSeq, rowOffset, FabricateRows(step, rowOffset));
                            pageSeqByResultSet[step.ResultSetId] = pageSeq + 1;
                            rowOffsetByResultSet[step.ResultSetId] = rowOffset + step.Rows;
                            break;
                        }

                        case "message":
                            yield return new ServerMessage("info", step.Number, step.Severity, step.Text ?? "message", step.Line);
                            break;

                        case "resultSetDone":
                            yield return new ResultSetCompleted(step.ResultSetId, step.RowCount);
                            break;

                        case "completed":
                            yield return new ExecCompleted([step.RowsAffected], step.Database);
                            break;

                        case "error":
                            throw new DbDriverException(
                                step.ErrorCode ?? Sts2ErrorCodes.QueryFailedServer,
                                step.Text ?? "Scripted server error.",
                                new ServerErrorDetail { Number = step.Number, Severity = step.Severity, State = 1 });

                        case "sever":
                            throw new DbDriverException(Sts2ErrorCodes.QueryFailedTransport, "Connection severed mid-stream.");

                        case "crash":
                            // Unclassified driver exception: the runner must map it to Sts2.Internal.
                            throw new InvalidOperationException("Scripted unclassified driver crash.");

                        case "hang":
                            await Task.Delay(Timeout.Infinite, linked.Token).ConfigureAwait(false);
                            break;

                        default:
                            throw new DbDriverException(Sts2ErrorCodes.Internal, "Unknown scripted step: " + step.Type);
                    }
                }
            }

            private static IReadOnlyList<ColumnInfo> FabricateColumns(int count, bool edgeValues)
            {
                var columns = new List<ColumnInfo>(count);
                for (int i = 0; i < count; i++)
                {
                    columns.Add(new ColumnInfo
                    {
                        Name = "col" + i,
                        EngineType = edgeValues ? "sql_variant" : (i == 0 ? "bigint" : "nvarchar"),
                        Nullable = i != 0,
                    });
                }
                return columns;
            }

            private static byte[] FabricateBlob(int bytes)
            {
                byte[] blob = new byte[bytes];
                for (int i = 0; i < bytes; i++)
                {
                    blob[i] = unchecked((byte)i);
                }
                return blob;
            }

            private static IReadOnlyList<IReadOnlyList<object?>> FabricateRows(FakeQueryStep step, long rowOffset)
            {
                var rows = new List<IReadOnlyList<object?>>(step.Rows);
                for (int r = 0; r < step.Rows; r++)
                {
                    long row = rowOffset + r;
                    var cells = new List<object?>(step.Columns);
                    for (int c = 0; c < step.Columns; c++)
                    {
                        if (step.EdgeValues)
                        {
                            // Deterministic typed-wrapper edge values (SPEC §7.7).
                            cells.Add(((row + c) % 4) switch
                            {
                                0 => System.Text.Json.Nodes.JsonNode.Parse("""{"$t":"decimal","v":"12.50"}"""),
                                1 => System.Text.Json.Nodes.JsonNode.Parse("""{"$t":"datetimeoffset","v":"2026-06-12T00:00:00+00:00"}"""),
                                2 => null, // DBNull -> JSON null
                                _ => System.Text.Json.Nodes.JsonNode.Parse("""{"$t":"binary","v":"AQID"}"""),
                            });
                        }
                        else if (c > 0 && step.CellValue is not null)
                        {
                            cells.Add(step.CellValue); // explicit wide/edge cell (STS2-3)
                        }
                        else if (c > 0 && step.CellBytes > 0)
                        {
                            // Deterministic wide cell of exactly CellBytes bytes (STS2-3).
                            cells.Add(step.CellBinary ? FabricateBlob(step.CellBytes) : new string('x', step.CellBytes));
                        }
                        else
                        {
                            cells.Add(c == 0 ? row : (object)("s" + row + "-" + c));
                        }
                    }
                    rows.Add(cells);
                }
                return rows;
            }

            public ValueTask CancelAsync(string queryId, CancellationToken cancellationToken)
            {
                queryCancel.Cancel();
                return ValueTask.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref disposed, 1) == 0)
                {
                    queryCancel.Cancel();
                    queryCancel.Dispose();
                    owner.SessionClosed();
                }
                return ValueTask.CompletedTask;
            }
        }
    }
}
