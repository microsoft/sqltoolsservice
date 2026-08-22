//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
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
        private const long JavaScriptMaxSafeInteger = 9_007_199_254_740_991L;

        private readonly SqliteConnection connection;
        private readonly Lock cancelGate = new();
        private CancellationTokenSource? currentQueryCancel;
        private string? currentQueryId;
        private int disposed;

        internal SqliteSession(SqliteConnection connection, ServerInfo server)
        {
            this.connection = connection;
            Server = server;
            int busyTimeoutMs = connection.DefaultTimeout >= int.MaxValue / 1000
                ? int.MaxValue
                : Math.Max(0, connection.DefaultTimeout * 1000);
            raw.sqlite3_busy_timeout(connection.Handle, busyTimeoutMs);
        }

        public ServerInfo Server { get; }

        public async IAsyncEnumerable<ExecEvent> ExecuteAsync(QueryExecuteRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            int pageRows = request.PageRows > 0 ? request.PageRows : Sts2Defaults.PageRows;
            int pageBytes = request.PageBytes > 0 ? request.PageBytes : Sts2Defaults.PageBytes;
            int maxCellBytes = request.MaxCellBytes > 0 ? request.MaxCellBytes : Sts2Defaults.MaxCellBytes;

            // A FRESH per-query cancellation source: cancelling one query must never stick to
            // the next (the old session-wide CTS made every query after a cancel insta-cancel — R016).
            var queryCancel = new CancellationTokenSource();
            ExecCompleted completion;
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
                // Microsoft.Data.Sqlite has no true async I/O. Yield before entering native
                // sqlite3_step so the caller can observe ExecStarted and signal cancellation
                // without its continuation being synchronously occupied by the query.
                await Task.Yield();

                using CancellationTokenRegistration cancelRegistration = linked.Token.Register(
                    static state => InterruptConnection((SqliteConnection)state!),
                    connection);
                linked.Token.ThrowIfCancellationRequested();

                int resultSetId = 0;
                long totalRowsAffected = 0;
                // Execute one SQLite-complete statement at a time. Besides preventing a
                // canceled provider reader from draining into a trailing mutation, the raw
                // statement exposes SQLite's native cell spans: Microsoft.Data.Sqlite's
                // GetChars/GetBytes APIs are documented as unsupported and otherwise either
                // materialize the full value or can fail in native metadata lookup.
                foreach (string statement in SplitStatements(request.Sql))
                {
                    linked.Token.ThrowIfCancellationRequested();
                    sqlite3_stmt? prepared = null;
                    int beforeChanges = raw.sqlite3_total_changes(connection.Handle);
                    try
                    {
                        int prepareResult = raw.sqlite3_prepare_v2(connection.Handle, statement, out prepared);
                        ThrowIfSqliteError(prepareResult, linked.Token);
                        if (prepared is null)
                        {
                            continue;
                        }

                        int firstStepResult = Step(prepared, linked.Token);
                        int fieldCount = raw.sqlite3_column_count(prepared);
                        if (fieldCount > 0)
                        {
                            foreach (ExecEvent execEvent in PumpResultSet(
                                prepared,
                                firstStepResult,
                                resultSetId,
                                pageRows,
                                pageBytes,
                                maxCellBytes,
                                linked.Token))
                            {
                                yield return execEvent;
                            }
                            resultSetId++;
                        }
                        else if (firstStepResult != raw.SQLITE_DONE)
                        {
                            throw new InvalidOperationException(
                                "SQLite returned a row for a statement without result columns.");
                        }
                    }
                    finally
                    {
                        prepared?.Dispose();
                    }

                    int afterChanges = raw.sqlite3_total_changes(connection.Handle);
                    // total_changes detects whether this statement changed rows without
                    // repeating the previous DML count for DDL. changes then preserves the
                    // provider's direct-row semantics (trigger side effects are excluded).
                    if (afterChanges != beforeChanges)
                    {
                        totalRowsAffected += Math.Max(0, raw.sqlite3_changes(connection.Handle));
                    }
                }
                completion = new ExecCompleted([totalRowsAffected]);
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

            // The terminal event is the connection-reuse boundary. Emit it only
            // after the provider reader/command and per-query state are gone.
            yield return completion;
        }

        /// <summary>Streams one result set page-by-page (no whole-result buffering — R016).</summary>
        private IEnumerable<ExecEvent> PumpResultSet(
            sqlite3_stmt statement,
            int firstStepResult,
            int resultSetId,
            int pageRows,
            int pageBytes,
            int maxCellBytes,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ColumnInfo> columns = ReadColumns(statement);
            yield return new ResultSetStarted(resultSetId, columns);

            int pageSeq = 0;
            long rowOffset = 0;
            long rowCount = 0;
            var page = new List<IReadOnlyList<object?>>(pageRows);
            long approximatePageBytes = 0;

            int stepResult = firstStepResult;
            while (stepResult == raw.SQLITE_ROW)
            {
                cancellationToken.ThrowIfCancellationRequested();
                object?[] cells = ReadCells(statement, maxCellBytes, cancellationToken);
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

                stepResult = Step(statement, cancellationToken);
            }

            if (page.Count > 0)
            {
                yield return new RowsPage(resultSetId, pageSeq, rowOffset, page);
            }
            yield return new ResultSetCompleted(resultSetId, rowCount);
        }

        private IReadOnlyList<ColumnInfo> ReadColumns(sqlite3_stmt statement)
        {
            int fieldCount = raw.sqlite3_column_count(statement);
            var columns = new List<ColumnInfo>(fieldCount);
            for (int i = 0; i < fieldCount; i++)
            {
                string? declaredType = raw.sqlite3_column_decltype(statement, i).utf8_to_string();
                columns.Add(new ColumnInfo
                {
                    Name = raw.sqlite3_column_name(statement, i).utf8_to_string() ?? $"column{i}",
                    EngineType = declaredType ?? StorageClassName(raw.sqlite3_column_type(statement, i)),
                    Nullable = ReadColumnNullability(statement, i),
                });
            }
            return columns;
        }

        private bool? ReadColumnNullability(sqlite3_stmt statement, int ordinal)
        {
            string? database = raw.sqlite3_column_database_name(statement, ordinal).utf8_to_string();
            string? table = raw.sqlite3_column_table_name(statement, ordinal).utf8_to_string();
            string? column = raw.sqlite3_column_origin_name(statement, ordinal).utf8_to_string();
            if (string.IsNullOrEmpty(database) || string.IsNullOrEmpty(table) || string.IsNullOrEmpty(column))
            {
                return null;
            }

            int result = raw.sqlite3_table_column_metadata(
                connection.Handle,
                database,
                table,
                column,
                out string? declaredType,
                out _,
                out int notNull,
                out int primaryKey,
                out _);
            if (result != raw.SQLITE_OK)
            {
                return null;
            }
            if (notNull != 0)
            {
                return false;
            }
            if (primaryKey == 0)
            {
                return true;
            }

            // In an ordinary rowid table, SQLite permits NULL in a non-INTEGER
            // PRIMARY KEY unless another constraint forbids it. INTEGER PRIMARY KEY
            // is the non-null rowid alias; other implicit PK cases are ambiguous from
            // this metadata API, so report unknown rather than a false guarantee.
            return string.Equals(declaredType, "INTEGER", StringComparison.OrdinalIgnoreCase)
                ? false
                : null;
        }

        private static string StorageClassName(int sqliteType) => sqliteType switch
        {
            raw.SQLITE_INTEGER => "INTEGER",
            raw.SQLITE_FLOAT => "REAL",
            raw.SQLITE_TEXT => "TEXT",
            raw.SQLITE_BLOB => "BLOB",
            raw.SQLITE_NULL => "NULL",
            _ => "UNKNOWN",
        };

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
                    DriverTruncatedValue value when value.Kind == "binary" =>
                        EstimateBinaryCellBytes(value.PrefixBytes?.Length ?? 0) + 160,
                    DriverTruncatedValue value =>
                        EstimateJsonStringBytes(value.PrefixText ?? string.Empty) + 160,
                    // Unsafe Int64 values use {"$t":"int64","v":"..."}; price the
                    // wrapper as well as the 20-digit decimal payload.
                    long value when value is > JavaScriptMaxSafeInteger or < -JavaScriptMaxSafeInteger => 64,
                    long => 24,
                    // Non-finite doubles use the runtime's typed wrapper
                    // {"$t":"double","v":"-Infinity"}, not a JSON number.
                    double value => double.IsFinite(value) ? 24 : 40,
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

        private int Step(sqlite3_stmt statement, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int result = raw.sqlite3_step(statement);
            ThrowIfSqliteError(result, cancellationToken);
            return result;
        }

        private void ThrowIfSqliteError(int result, CancellationToken cancellationToken)
        {
            if (result is raw.SQLITE_OK or raw.SQLITE_ROW or raw.SQLITE_DONE)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                SqliteException.ThrowExceptionForRC(result, connection.Handle);
            }
            catch (SqliteException ex)
            {
                throw Classify(ex);
            }
            throw new DbDriverException(
                Sts2ErrorCodes.QueryFailedServer,
                $"SQLite query failed with result code {result}.",
                new ServerErrorDetail { Number = result, Severity = 16, State = 1 });
        }

        private static DbDriverException Classify(SqliteException ex) =>
            new(Sts2ErrorCodes.QueryFailedServer, ex.Message,
                new ServerErrorDetail { Number = ex.SqliteErrorCode, Severity = 16, State = 1 });

        /// <summary>
        /// Returns one cell as a plain CLR value (long, double, string, byte[], or null).
        /// Wire encoding — JSON natives vs typed wrappers (SPEC §7.7) — is the runner's job;
        /// the port stays free of JSON types.
        /// </summary>
        private static object?[] ReadCells(
            sqlite3_stmt statement,
            int maxCellBytes,
            CancellationToken cancellationToken)
        {
            int fieldCount = raw.sqlite3_column_count(statement);
            var cells = new object?[fieldCount];
            for (int ordinal = 0; ordinal < fieldCount; ordinal++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cells[ordinal] = raw.sqlite3_column_type(statement, ordinal) switch
                {
                    raw.SQLITE_NULL => null,
                    raw.SQLITE_INTEGER => raw.sqlite3_column_int64(statement, ordinal),
                    raw.SQLITE_FLOAT => raw.sqlite3_column_double(statement, ordinal),
                    raw.SQLITE_TEXT =>
                        SqliteLargeValueReader.ReadText(
                            statement,
                            ordinal,
                            maxCellBytes,
                            cancellationToken),
                    raw.SQLITE_BLOB =>
                        SqliteLargeValueReader.ReadBinary(
                            statement,
                            ordinal,
                            maxCellBytes,
                            cancellationToken),
                    _ => throw new InvalidOperationException("Unknown SQLite storage class."),
                };
            }
            return cells;
        }

        /// <summary>
        /// Splits a provider batch only where SQLite itself says the prefix is a complete
        /// statement. This respects quoted semicolons, comments, and trigger bodies without
        /// maintaining a second SQL grammar in the driver.
        /// </summary>
        private static IEnumerable<string> SplitStatements(string sql)
        {
            int start = 0;
            for (int i = 0; i < sql.Length; i++)
            {
                if (sql[i] != ';')
                {
                    continue;
                }

                string candidate = sql[start..(i + 1)];
                if (raw.sqlite3_complete(candidate) != 0)
                {
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        yield return candidate;
                    }
                    start = i + 1;
                }
            }

            if (start < sql.Length)
            {
                string tail = sql[start..];
                if (!string.IsNullOrWhiteSpace(tail))
                {
                    yield return tail;
                }
            }
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
