//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;
using SQLitePCL;

namespace Microsoft.SqlTools.Sts2.Drivers.Sqlite
{
    /// <summary>One open Sqlite session. Owns the connection for its lifetime (SPEC §10.3).</summary>
    internal sealed class SqliteSession : IDbSession
    {
        private readonly SqliteConnection connection;
        private readonly Lock cancelGate = new();
        private CancellationTokenSource? currentQueryCancel;
        private string? currentQueryId;
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
            int pageBytes = request.PageBytes > 0 ? request.PageBytes : Sts2Defaults.PageBytes;

            // A FRESH per-query cancellation source: cancelling one query must never stick to
            // the next (the old session-wide CTS made every query after a cancel insta-cancel — R016).
            var queryCancel = new CancellationTokenSource();
            try
            {
                // Install query state inside the owning try/finally. If the consumer
                // disposes the iterator after ExecStarted, the finally still clears
                // and disposes the published cancellation source.
                lock (cancelGate)
                {
                    currentQueryCancel = queryCancel;
                    currentQueryId = request.QueryId;
                }
                using CancellationTokenSource linked = CreateQueryCancellationSource(
                    cancellationToken,
                    queryCancel.Token,
                    request.QueryTimeoutMs);

                yield return new ExecStarted(request.QueryId);

                await using SqliteCommand command = connection.CreateCommand();
                command.CommandText = request.Sql;
                using CancellationTokenRegistration cancelRegistration = linked.Token.Register(
                    static state => InterruptConnection((SqliteConnection)state!),
                    connection);
                linked.Token.ThrowIfCancellationRequested();

                SqliteDataReader reader;
                try
                {
                    reader = await command.ExecuteReaderAsync(linked.Token).ConfigureAwait(false);
                }
                catch (SqliteException ex)
                {
                    linked.Token.ThrowIfCancellationRequested();
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
                            await foreach (ExecEvent execEvent in PumpResultSetAsync(reader, resultSetId, pageRows, pageBytes, linked.Token).ConfigureAwait(false))
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
                        currentQueryId = null;
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
            SqliteDataReader reader, int resultSetId, int pageRows, int pageBytes, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IReadOnlyList<ColumnInfo> columns = ReadColumns(reader, cancellationToken);
            yield return new ResultSetStarted(resultSetId, columns);

            int pageSeq = 0;
            long rowOffset = 0;
            long rowCount = 0;
            var page = new List<IReadOnlyList<object?>>(pageRows);
            long approximatePageBytes = 0;

            while (true)
            {
                object?[]? cells = await ReadRowAsync(reader, cancellationToken).ConfigureAwait(false);
                if (cells is null)
                {
                    break;
                }
                long rowBytes = EstimateRowBytes(cells);
                if (page.Count > 0 && approximatePageBytes + rowBytes > pageBytes)
                {
                    yield return new RowsPage(resultSetId, pageSeq, rowOffset, page);
                    rowOffset += page.Count;
                    pageSeq++;
                    page = new List<IReadOnlyList<object?>>(pageRows);
                    approximatePageBytes = 0;
                }
                page.Add(cells);
                approximatePageBytes += rowBytes;
                rowCount++;

                if (page.Count >= pageRows || approximatePageBytes >= pageBytes)
                {
                    yield return new RowsPage(resultSetId, pageSeq, rowOffset, page);
                    rowOffset += page.Count;
                    pageSeq++;
                    page = new List<IReadOnlyList<object?>>(pageRows);
                    approximatePageBytes = 0;
                }
            }

            if (page.Count > 0)
            {
                yield return new RowsPage(resultSetId, pageSeq, rowOffset, page);
            }
            yield return new ResultSetCompleted(resultSetId, rowCount);
        }

        private static IReadOnlyList<ColumnInfo> ReadColumns(
            SqliteDataReader reader,
            CancellationToken cancellationToken)
        {
            try
            {
                System.Collections.ObjectModel.ReadOnlyCollection<DbColumn> schema = reader.GetColumnSchema();
                var columns = new List<ColumnInfo>(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    DbColumn column = schema[i];
                    columns.Add(new ColumnInfo
                    {
                        Name = column.ColumnName ?? reader.GetName(i),
                        EngineType = column.DataTypeName ?? reader.GetDataTypeName(i),
                        Nullable = column.AllowDBNull,
                    });
                }
                return columns;
            }
            catch (SqliteException ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw Classify(ex);
            }
        }

        private static long EstimateRowBytes(IReadOnlyList<object?> cells)
        {
            long total = 2;
            foreach (object? cell in cells)
            {
                total += 1 + (cell switch
                {
                    null => 4,
                    string value => EstimateJsonStringBytes(value),
                    // The runtime emits {"$t":"binary","v":"..."}, not a
                    // bare base64 JSON string. Include conservative wrapper space.
                    byte[] value => EstimateBinaryCellBytes(value.Length),
                    long or double => 24,
                    _ => 24,
                });
            }
            return total;
        }

        private static long EstimateBinaryCellBytes(int byteLength) =>
            (((long)byteLength + 2) / 3 * 4) + 32;

        private static long EstimateJsonStringBytes(ReadOnlySpan<char> value)
        {
            long total = 2;
            foreach (char c in value)
            {
                total += c < 128 && !JavaScriptEncoder.Default.WillEncode(c) ? 1 : 6;
            }
            return total;
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
                cancellationToken.ThrowIfCancellationRequested();
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
                cancellationToken.ThrowIfCancellationRequested();
                throw Classify(ex);
            }
        }

        private static DbDriverException Classify(SqliteException ex) =>
            new(Sts2ErrorCodes.QueryFailedServer, ex.Message,
                new ServerErrorDetail { Number = ex.SqliteErrorCode, Severity = 16, State = 1 });

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

        private static void InterruptConnection(SqliteConnection connection)
        {
            try
            {
                // Microsoft.Data.Sqlite.SqliteCommand.Cancel is documented as a no-op.
                // sqlite3_interrupt is the provider-supported, thread-safe way to abort
                // a native query from the timeout/cancel callback.
                raw.sqlite3_interrupt(connection.Handle);
            }
            catch (InvalidOperationException)
            {
                // Connection already closed/disposed.
            }
        }

        private static CancellationTokenSource CreateQueryCancellationSource(
            CancellationToken pumpCancellation,
            CancellationToken explicitQueryCancellation,
            int queryTimeoutMs)
        {
            CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
                pumpCancellation,
                explicitQueryCancellation);
            if (queryTimeoutMs > 0)
            {
                linked.CancelAfter(queryTimeoutMs);
            }
            return linked;
        }

        public ValueTask CancelAsync(string queryId, CancellationToken cancellationToken)
        {
            // A delayed cancellation for an old query must not interrupt the
            // current query on this reusable session.
            lock (cancelGate)
            {
                if (string.Equals(currentQueryId, queryId, StringComparison.Ordinal))
                {
                    currentQueryCancel?.Cancel();
                }
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
