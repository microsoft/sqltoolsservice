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
        private int disposed;

        private sealed record ActiveQuery(
            string QueryId,
            SqlCommand Command,
            CancellationTokenSource ExplicitCancellation);

        internal SqlClientSession(SqlConnection connection, ServerInfo server)
        {
            this.connection = connection;
            Server = server;
        }

        public ServerInfo Server { get; }

        public async IAsyncEnumerable<ExecEvent> ExecuteAsync(QueryExecuteRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
            int pageRows = request.PageRows > 0 ? request.PageRows : Sts2Defaults.PageRows;
            int pageBytes = request.PageBytes > 0 ? request.PageBytes : Sts2Defaults.PageBytes;
            int maxCellBytes = request.MaxCellBytes > 0 ? request.MaxCellBytes : Sts2Defaults.MaxCellBytes;

            ExecCompleted completion;
            await using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = request.Sql;
                if (request.QueryTimeoutMs > 0)
                {
                    command.CommandTimeout = SqlClientConnectionString.ToProviderSeconds(request.QueryTimeoutMs);
                }
                // Publish query-local cancellation before ExecStarted. An IDbSession caller is
                // allowed to cancel as soon as it observes that event; publishing only on the
                // following iterator move loses that cancellation and can start the command.
                using var explicitCancellation = new CancellationTokenSource();
                using CancellationTokenSource queryCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        explicitCancellation.Token);
                var publishedQuery = new ActiveQuery(request.QueryId, command, explicitCancellation);
                if (Interlocked.CompareExchange(ref activeQuery, publishedQuery, null) is not null)
                {
                    throw new InvalidOperationException("SqlClientSession permits one active query.");
                }
                if (Volatile.Read(ref disposed) != 0)
                {
                    Interlocked.CompareExchange(ref activeQuery, null, publishedQuery);
                    throw new ObjectDisposedException(nameof(SqlClientSession));
                }
                // Info-class engine messages (PRINT, RAISERROR severity <= 10, DBCC output)
                // are raised on InfoMessage while the reader pumps the TDS stream (SPEC §10.2:
                // map info messages to ServerMessage). Text passes through verbatim. Queue and
                // drain at provider/row boundaries so messages hold stream order relative to
                // result rows as well as result sets.
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
                    yield return new ExecStarted(request.QueryId);

                    // SqlCommand.Cancel is synchronous provider code and has blocked in provider
                    // edge cases. Never run it inline on CancellationTokenSource.Cancel(), which
                    // is also the coordinator's terminal-ack path. Registration still happens
                    // before the first provider await, and an already-canceled token queues the
                    // callback immediately.
                    using CancellationTokenRegistration cancelRegistration =
                        RegisterProviderCancellation(queryCancellation.Token, command.Cancel);
                    queryCancellation.Token.ThrowIfCancellationRequested();

                    connection.InfoMessage += onInfoMessage;
                    try
                    {
                        SqlDataReader reader = await OpenReaderAsync(command, queryCancellation.Token).ConfigureAwait(false);
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
                                    await foreach (ExecEvent execEvent in PumpResultSetAsync(
                                        reader,
                                        resultSetId,
                                        pageRows,
                                        pageBytes,
                                        maxCellBytes,
                                        request.VectorBinary,
                                        request.SpatialWkb,
                                        pendingMessages,
                                        queryCancellation.Token).ConfigureAwait(false))
                                    {
                                        yield return execEvent;
                                    }
                                    resultSetId++;
                                }
                                else if (reader.RecordsAffected > 0)
                                {
                                    totalAffected += reader.RecordsAffected;
                                }
                                more = await NextResultAsync(reader, queryCancellation.Token).ConfigureAwait(false);
                            }
                            while (more);

                            while (pendingMessages.TryDequeue(out ServerMessage? pending))
                            {
                                yield return pending;
                            }
                            // connection.Database tracks ENVCHANGE, so a USE inside
                            // the batch is reflected here — the client's database
                            // source of truth on completion.
                            completion = new ExecCompleted(
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

            // Publish the terminal event only after the provider reader and command
            // are disposed and the connection is no longer marked active. Core may
            // allow the next query to start as soon as it observes this event.
            yield return completion;
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
            SqlDataReader reader,
            int resultSetId,
            int pageRows,
            int pageBytes,
            int maxCellBytes,
            bool vectorBinary,
            bool spatialWkb,
            ConcurrentQueue<ServerMessage> pendingMessages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ColumnReadPlan columnPlan = await ReadColumnsAsync(reader, spatialWkb, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<ColumnInfo> columns = columnPlan.Columns;
            // QO-4: MAX-typed columns stream under SequentialAccess — bounded
            // prefix + streaming digest/byte count, never full materialization.
            // D-0018/D-0019: vector and CLR UDT columns route to dedicated reads.
            SqlLargeValueReader.CellRead[] readKinds = SqlLargeValueReader.ClassifyColumns(columns, vectorBinary, spatialWkb);
            SqlLargeValueReader.ApplyProviderUdtMetadata(readKinds, columnPlan.ProviderClrUdts);
            // A provider may consume an informational token while resolving schema. Keep such a
            // message ahead of the result-set notification it preceded on the TDS stream.
            while (pendingMessages.TryDequeue(out ServerMessage? schemaMessage))
            {
                yield return schemaMessage;
            }
            yield return new ResultSetStarted(resultSetId, columns);

            int pageSeq = 0;
            long rowOffset = 0;
            long rowCount = 0;
            // QO-3: rows and bytes both bound page construction — whichever limit
            // is reached first completes the page (SqlRowsPageBuilder).
            var builder = new SqlRowsPageBuilder(pageRows, pageBytes);

            IEnumerable<ExecEvent> FlushRowsThenMessages()
            {
                IReadOnlyList<IReadOnlyList<object?>>? partialPage = builder.Flush();
                if (partialPage is not null)
                {
                    yield return new RowsPage(resultSetId, pageSeq, rowOffset, partialPage);
                    rowOffset += partialPage.Count;
                    pageSeq++;
                }
                while (pendingMessages.TryDequeue(out ServerMessage? message))
                {
                    yield return message;
                }
            }

            while (true)
            {
                bool hasRow = await ReadRowAsync(reader, cancellationToken).ConfigureAwait(false);
                if (!hasRow)
                {
                    break;
                }

                // InfoMessage fires while ReadAsync advances the TDS stream. A message observed
                // there belongs after already-buffered rows and before the row now exposed.
                if (!pendingMessages.IsEmpty)
                {
                    foreach (ExecEvent boundaryEvent in FlushRowsThenMessages())
                    {
                        yield return boundaryEvent;
                    }
                }

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

                // Sequential large-value reads can advance beyond the current row and surface an
                // informational token. Publish the row first, then the message, without holding
                // either until the entire result set completes.
                if (!pendingMessages.IsEmpty)
                {
                    foreach (ExecEvent boundaryEvent in FlushRowsThenMessages())
                    {
                        yield return boundaryEvent;
                    }
                }
            }

            // ReadAsync(false) can itself consume trailing informational tokens. Pending rows
            // precede those tokens; both precede resultSetDone.
            foreach (ExecEvent boundaryEvent in FlushRowsThenMessages())
            {
                yield return boundaryEvent;
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
                    cancellationToken.ThrowIfCancellationRequested();
                    cells[i] = reader.IsDBNull(i)
                        ? null
                        : readKinds[i] switch
                        {
                            SqlLargeValueReader.CellRead.Text =>
                                columns[i].EngineType.Equals("vector", StringComparison.OrdinalIgnoreCase)
                                    ? SqlClientVectorValueReader.ReadText(reader, i)
                                    : SqlLargeValueReader.ReadText(reader, i, maxCellBytes, cancellationToken),
                            SqlLargeValueReader.CellRead.Binary =>
                                SqlLargeValueReader.ReadBinary(reader, i, maxCellBytes, cancellationToken),
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
            catch (Exception)
            {
                // Cancellation is best-effort provider cleanup. It must not replace the
                // query's stable terminal cancellation with an arbitrary provider failure.
            }
        }

        /// <summary>
        /// Registers synchronous provider cancellation without ever invoking provider code on
        /// the thread that signals the token. Provider cancellation is best effort; blocking or
        /// throwing callbacks are isolated on a worker and cannot delay terminal acknowledgement.
        /// </summary>
        internal static CancellationTokenRegistration RegisterProviderCancellation(
            CancellationToken cancellationToken,
            Action providerCancel)
        {
            ArgumentNullException.ThrowIfNull(providerCancel);
            return cancellationToken.Register(
                static state => QueueProviderCancellation((Action)state!),
                providerCancel);
        }

        private static void QueueProviderCancellation(Action providerCancel)
        {
            ThreadPool.UnsafeQueueUserWorkItem(
                static callback =>
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception)
                    {
                        // The query pump observes its cancellation token and owns the terminal
                        // result. Provider cancellation is only an interrupt accelerator.
                    }
                },
                providerCancel,
                preferLocal: false);
        }

        public ValueTask CancelAsync(string queryId, CancellationToken cancellationToken)
        {
            // A delayed cancel for a completed query must not cancel the next
            // command on the same session. The streaming loop also observes its
            // own cancellation token.
            ActiveQuery? query = Volatile.Read(ref activeQuery);
            if (query is not null && string.Equals(query.QueryId, queryId, StringComparison.Ordinal))
            {
                try
                {
                    // The linked execution token owns the stable canceled terminal; its provider
                    // callback is queued off-thread by RegisterProviderCancellation.
                    query.ExplicitCancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // A completion/disposal race already made this cancellation obsolete.
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
            ActiveQuery? query = Volatile.Read(ref activeQuery);
            if (query is not null)
            {
                CancelCommand(query.Command);
            }
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
