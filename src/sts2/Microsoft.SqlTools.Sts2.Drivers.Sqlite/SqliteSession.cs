//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Drivers.Sqlite
{
    /// <summary>One open Sqlite session. Owns the connection for its lifetime (SPEC §10.3).</summary>
    internal sealed class SqliteSession : IDbSession
    {
        private readonly SqliteConnection connection;
        private readonly Lock cancelGate = new();
        private CancellationTokenSource? currentQueryCancel;
        private int disposed;

        internal SqliteSession(SqliteConnection connection, ServerInfo server)
        {
            this.connection = connection;
            Server = server;
        }

        public ServerInfo Server { get; }

        public async IAsyncEnumerable<ExecEvent> ExecuteAsync(QueryExecuteRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            int pageRows = request.PageRows > 0 ? request.PageRows : Sts2Defaults.PageRows;

            // A FRESH per-query cancellation source: cancelling one query must never stick to
            // the next (the old session-wide CTS made every query after a cancel insta-cancel — R016).
            var queryCancel = new CancellationTokenSource();
            lock (cancelGate)
            {
                currentQueryCancel = queryCancel;
            }
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, queryCancel.Token);

            yield return new ExecStarted(request.QueryId);

            try
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.CommandText = request.Sql;

                SqliteDataReader reader;
                try
                {
                    reader = await command.ExecuteReaderAsync(linked.Token).ConfigureAwait(false);
                }
                catch (SqliteException ex)
                {
                    throw Classify(ex);
                }

                await using (reader.ConfigureAwait(false))
                {
                    int resultSetId = 0;
                    long totalRowsAffected = 0;
                    bool hasResultSet;
                    do
                    {
                        if (reader.FieldCount > 0)
                        {
                            await foreach (ExecEvent execEvent in PumpResultSetAsync(reader, resultSetId, pageRows, linked.Token).ConfigureAwait(false))
                            {
                                yield return execEvent;
                            }
                            resultSetId++;
                        }
                        else
                        {
                            totalRowsAffected += reader.RecordsAffected >= 0 ? reader.RecordsAffected : 0;
                        }

                        hasResultSet = await NextResultAsync(reader, linked.Token).ConfigureAwait(false);
                    }
                    while (hasResultSet);

                    yield return new ExecCompleted([reader.RecordsAffected >= 0 ? reader.RecordsAffected : totalRowsAffected]);
                }
            }
            finally
            {
                lock (cancelGate)
                {
                    if (currentQueryCancel == queryCancel)
                    {
                        currentQueryCancel = null;
                    }
                }
                queryCancel.Dispose();
            }
        }

        /// <summary>
        /// Streams one result set page-by-page (no whole-result buffering — R016). Each row
        /// read is wrapped for the SqliteException boundary in <see cref="ReadRowAsync"/> so
        /// the iterator can yield each page outside any try/catch (which C# forbids combining).
        /// </summary>
        private static async IAsyncEnumerable<ExecEvent> PumpResultSetAsync(
            SqliteDataReader reader, int resultSetId, int pageRows, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var columns = new List<ColumnInfo>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetName(i),
                    EngineType = reader.GetDataTypeName(i),
                    Nullable = true,
                });
            }
            yield return new ResultSetStarted(resultSetId, columns);

            int pageSeq = 0;
            long rowOffset = 0;
            long rowCount = 0;
            var page = new List<IReadOnlyList<object?>>(pageRows);

            while (true)
            {
                object?[]? cells = await ReadRowAsync(reader, cancellationToken).ConfigureAwait(false);
                if (cells is null)
                {
                    break;
                }
                page.Add(cells);
                rowCount++;

                if (page.Count >= pageRows)
                {
                    yield return new RowsPage(resultSetId, pageSeq, rowOffset, page);
                    rowOffset += page.Count;
                    pageSeq++;
                    page = new List<IReadOnlyList<object?>>(pageRows);
                }
            }

            if (page.Count > 0)
            {
                yield return new RowsPage(resultSetId, pageSeq, rowOffset, page);
            }
            yield return new ResultSetCompleted(resultSetId, rowCount);
        }

        /// <summary>Reads one row's cells, or null at end of result set. Classifies Sqlite faults.</summary>
        private static async Task<object?[]?> ReadRowAsync(SqliteDataReader reader, CancellationToken cancellationToken)
        {
            try
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }
                var cells = new object?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    cells[i] = EncodeCell(reader, i);
                }
                return cells;
            }
            catch (SqliteException ex)
            {
                throw Classify(ex);
            }
        }

        private static async Task<bool> NextResultAsync(SqliteDataReader reader, CancellationToken cancellationToken)
        {
            try
            {
                return await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqliteException ex)
            {
                throw Classify(ex);
            }
        }

        private static DbDriverException Classify(SqliteException ex) =>
            new(Sts2ErrorCodes.QueryFailedServer, ex.Message,
                new ServerErrorDetail { Number = ex.SqliteErrorCode, Severity = 16, State = 1 }, ex);

        /// <summary>
        /// Returns one cell as a plain CLR value (long, double, string, byte[], or null).
        /// Wire encoding — JSON natives vs typed wrappers (SPEC §7.7) — is the runner's job;
        /// the port stays free of JSON types.
        /// </summary>
        private static object? EncodeCell(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return reader.GetFieldType(ordinal) switch
            {
                Type t when t == typeof(long) => reader.GetInt64(ordinal),
                Type t when t == typeof(double) => reader.GetDouble(ordinal),
                Type t when t == typeof(string) => reader.GetString(ordinal),
                Type t when t == typeof(byte[]) => reader.GetValue(ordinal),
                _ => reader.GetValue(ordinal),
            };
        }

        public ValueTask CancelAsync(string queryId, CancellationToken cancellationToken)
        {
            // Cooperative cancellation of the CURRENT query only; honored between pages and reads.
            lock (cancelGate)
            {
                currentQueryCancel?.Cancel();
            }
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }
            lock (cancelGate)
            {
                currentQueryCancel?.Cancel();
            }
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
