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
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>One open SQL Server session (SPEC §10.2). Page-by-page streaming, real cancel.</summary>
    internal sealed class SqlClientSession : IDbSession
    {
        private readonly SqlConnection connection;
        private ActiveQuery? activeQuery;

        private sealed record ActiveQuery(string QueryId, SqlCommand Command);

        internal SqlClientSession(SqlConnection connection, ServerInfo server)
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
            int maxCellBytes = request.MaxCellBytes > 0 ? request.MaxCellBytes : Sts2Defaults.MaxCellBytes;

            yield return new ExecStarted(request.QueryId);

            await using SqlCommand command = connection.CreateCommand();
            command.CommandText = request.Sql;
            if (request.QueryTimeoutMs > 0)
            {
                command.CommandTimeout = SqlClientConnectionString.ToProviderSeconds(request.QueryTimeoutMs);
            }
            // Publish the command atomically for the provider-level CancelAsync path. A cancel
            // can arrive after the query pump is registered but before ExecuteReaderAsync starts.
            var publishedQuery = new ActiveQuery(request.QueryId, command);
            Volatile.Write(ref activeQuery, publishedQuery);
            // Info-class engine messages (PRINT, RAISERROR severity <= 10, DBCC output)
            // are raised on InfoMessage while the reader pumps the TDS stream (SPEC §10.2:
            // map info messages to ServerMessage). Text passes through verbatim. Queue and
            // drain at pump boundaries so messages hold stream order relative to result sets.
            var pendingMessages = new ConcurrentQueue<ServerMessage>();
            SqlInfoMessageEventHandler onInfoMessage = (_, args) =>
            {
                foreach (SqlError error in args.Errors)
                {
                    pendingMessages.Enqueue(new ServerMessage(
                        "info", error.Number, error.Class, error.Message,
                        error.LineNumber > 0 ? error.LineNumber : null));
                }
            };
            // Clear activeQuery in finally so a faulted reader/row read never leaves it
            // pointing at a disposed command (R035). The finally runs when the enumerator is
            // disposed — on completion, break, or exception. The InfoMessage unsubscribe
            // rides the same finally (SPEC §10.2: event handlers unsubscribed).
            try
            {
                // Bind the pump token directly to the command. Register invokes immediately
                // when the token was already canceled. Command ownership and activeCommand
                // cleanup already surround this path, including that immediate callback.
                using CancellationTokenRegistration cancelRegistration = cancellationToken.Register(
                    static state => CancelCommand((SqlCommand)state!),
                    command);
                cancellationToken.ThrowIfCancellationRequested();

                connection.InfoMessage += onInfoMessage;
                try
                {
                    SqlDataReader reader = await OpenReaderAsync(command, cancellationToken).ConfigureAwait(false);
                    await using (reader.ConfigureAwait(false))
                    {
                        int resultSetId = 0;
                        long totalAffected = 0;
                        bool more;
                        do
                        {
                            while (pendingMessages.TryDequeue(out ServerMessage? pending))
                            {
                                yield return pending;
                            }
                            if (reader.FieldCount > 0)
                            {
                                await foreach (ExecEvent execEvent in PumpResultSetAsync(reader, resultSetId, pageRows, pageBytes, maxCellBytes, request.VectorBinary, request.SpatialWkb, cancellationToken).ConfigureAwait(false))
                                {
                                    yield return execEvent;
                                }
                                resultSetId++;
                            }
                            else if (reader.RecordsAffected > 0)
                            {
                                totalAffected += reader.RecordsAffected;
                            }
                            more = await NextResultAsync(reader, cancellationToken).ConfigureAwait(false);
                        }
                        while (more);

                        while (pendingMessages.TryDequeue(out ServerMessage? pending))
                        {
                            yield return pending;
                        }
                        // connection.Database tracks ENVCHANGE, so a USE inside
                        // the batch is reflected here — the client's database
                        // source of truth on completion.
                        yield return new ExecCompleted(
                            [reader.RecordsAffected >= 0 ? reader.RecordsAffected : totalAffected],
                            connection.Database);
                    }
                }
                finally
                {
                    connection.InfoMessage -= onInfoMessage;
                }
            }
            finally
            {
                // Do not let a late cleanup from an old enumerator clear a newer
                // query that has already published its command.
                Interlocked.CompareExchange(ref activeQuery, null, publishedQuery);
            }
        }

        private static async Task<SqlDataReader> OpenReaderAsync(SqlCommand command, CancellationToken cancellationToken)
        {
            try
            {
                // SequentialAccess (QO-4): cells are read in ordinal order (the
                // pump already does) and large values can STREAM — bounded
                // prefix + streaming digest instead of full materialization.
                return await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                throw ClassifyQueryFailure(ex, cancellationToken);
            }
        }

        private static async Task<bool> NextResultAsync(SqlDataReader reader, CancellationToken cancellationToken)
        {
            try
            {
                return await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                throw ClassifyQueryFailure(ex, cancellationToken);
            }
        }

        /// <summary>Streams one result set page-by-page (no full-result buffering).</summary>
        private static async IAsyncEnumerable<ExecEvent> PumpResultSetAsync(
            SqlDataReader reader, int resultSetId, int pageRows, int pageBytes, int maxCellBytes, bool vectorBinary, bool spatialWkb, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ColumnReadPlan columnPlan = await ReadColumnsAsync(reader, spatialWkb, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<ColumnInfo> columns = columnPlan.Columns;
            // QO-4: MAX-typed columns stream under SequentialAccess — bounded
            // prefix + streaming digest/byte count, never full materialization.
            // D-0018/D-0019: vector and CLR UDT columns route to dedicated reads.
            SqlLargeValueReader.CellRead[] readKinds = SqlLargeValueReader.ClassifyColumns(columns, vectorBinary, spatialWkb);
            SqlLargeValueReader.ApplyProviderUdtMetadata(readKinds, columnPlan.ProviderClrUdts);
            yield return new ResultSetStarted(resultSetId, columns);

            int pageSeq = 0;
            long rowOffset = 0;
            long rowCount = 0;
            // QO-3: rows and bytes both bound page construction — whichever limit
            // is reached first completes the page (SqlRowsPageBuilder).
            var builder = new SqlRowsPageBuilder(pageRows, pageBytes);

            while (await ReadRowAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                object?[] cells = ReadCells(
                    reader,
                    readKinds,
                    columns,
                    maxCellBytes,
                    cancellationToken);
                rowCount++;
                foreach (IReadOnlyList<IReadOnlyList<object?>> page in builder.Add(cells))
                {
                    yield return new RowsPage(resultSetId, pageSeq, rowOffset, page);
                    rowOffset += page.Count;
                    pageSeq++;
                }
            }

            IReadOnlyList<IReadOnlyList<object?>>? tail = builder.Flush();
            if (tail is not null)
            {
                yield return new RowsPage(resultSetId, pageSeq, rowOffset, tail);
            }
            yield return new ResultSetCompleted(resultSetId, rowCount);
        }

        private sealed record ColumnReadPlan(
            IReadOnlyList<ColumnInfo> Columns,
            IReadOnlyList<bool> ProviderClrUdts);

        private static async Task<ColumnReadPlan> ReadColumnsAsync(
            SqlDataReader reader,
            bool spatialWkb,
            CancellationToken cancellationToken)
        {
            try
            {
                var columns = new List<ColumnInfo>(reader.FieldCount);
                var providerClrUdts = new List<bool>(reader.FieldCount);
                System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Common.DbColumn> schema =
                    await reader.GetColumnSchemaAsync().ConfigureAwait(false);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var column = schema[i];
                    string engineType = column.DataTypeName ?? reader.GetDataTypeName(i);
                    providerClrUdts.Add(!string.IsNullOrEmpty(column.UdtAssemblyQualifiedName));
                    string? spatialKind = spatialWkb ? SqlLargeValueReader.SpatialKind(engineType) : null;
                    columns.Add(new ColumnInfo
                    {
                        Name = column.ColumnName ?? reader.GetName(i),
                        EngineType = engineType,
                        Nullable = column.AllowDBNull,
                        Precision = column.NumericPrecision,
                        Scale = column.NumericScale,
                        Length = column.ColumnSize,
                        Collation = null,
                        SpatialKind = spatialKind,
                        SpatialEncoding = spatialKind is null ? null : "wkb-v1",
                    });
                }
                return new ColumnReadPlan(columns, providerClrUdts);
            }
            catch (SqlException ex)
            {
                throw ClassifyQueryFailure(ex, cancellationToken);
            }
        }

        private static object?[] ReadCells(
            SqlDataReader reader,
            IReadOnlyList<SqlLargeValueReader.CellRead> readKinds,
            IReadOnlyList<ColumnInfo> columns,
            int maxCellBytes,
            CancellationToken cancellationToken)
        {
            try
            {
                var cells = new object?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    cells[i] = reader.IsDBNull(i)
                        ? null
                        : readKinds[i] switch
                        {
                            SqlLargeValueReader.CellRead.Text =>
                                columns[i].EngineType.Equals("vector", StringComparison.OrdinalIgnoreCase)
                                    ? SqlClientVectorValueReader.ReadText(reader, i)
                                    : SqlLargeValueReader.ReadText(reader, i, maxCellBytes),
                            SqlLargeValueReader.CellRead.Binary =>
                                SqlLargeValueReader.ReadBinary(reader, i, maxCellBytes),
                            SqlLargeValueReader.CellRead.Vector =>
                                SqlClientVectorValueReader.Read(reader, i, maxCellBytes),
                            SqlLargeValueReader.CellRead.Spatial =>
                                SqlClientSpatialValueReader.Read(
                                    reader,
                                    i,
                                    columns[i].SpatialKind ?? "unknown",
                                    maxCellBytes),
                            _ => reader.GetValue(i),
                        };
                }
                return cells;
            }
            catch (SqlException ex)
            {
                throw ClassifyQueryFailure(ex, cancellationToken);
            }
        }

        private static async Task<bool> ReadRowAsync(SqlDataReader reader, CancellationToken cancellationToken)
        {
            try
            {
                return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                throw ClassifyQueryFailure(ex, cancellationToken);
            }
        }

        private static DbDriverException ClassifyQueryFailure(
            SqlException ex,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new DbDriverException(
                SqlClientErrorMapping.ClassifyQuery(ex),
                ex.Message,
                SqlClientErrorMapping.ServerDetail(ex));
        }

        private static void CancelCommand(SqlCommand command)
        {
            try
            {
                command.Cancel();
            }
            catch (InvalidOperationException)
            {
                // Command already completed/disposed; nothing to cancel.
            }
        }

        public ValueTask CancelAsync(string queryId, CancellationToken cancellationToken)
        {
            // A delayed cancel for a completed query must not cancel the next
            // command on the same session. The streaming loop also observes its
            // own cancellation token.
            ActiveQuery? query = Volatile.Read(ref activeQuery);
            if (query is not null && string.Equals(query.QueryId, queryId, StringComparison.Ordinal))
            {
                CancelCommand(query.Command);
            }
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            ActiveQuery? query = Volatile.Read(ref activeQuery);
            if (query is not null)
            {
                CancelCommand(query.Command);
            }
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
