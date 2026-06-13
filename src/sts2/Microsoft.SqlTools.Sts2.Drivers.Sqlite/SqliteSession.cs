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
        private readonly CancellationTokenSource activeQueryCancel = new();
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
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, activeQueryCancel.Token);
            int pageRows = request.PageRows > 0 ? request.PageRows : Sts2Defaults.PageRows;

            yield return new ExecStarted(request.QueryId);

            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = request.Sql;

            SqliteDataReader reader;
            try
            {
                reader = await command.ExecuteReaderAsync(linked.Token).ConfigureAwait(false);
            }
            catch (SqliteException ex)
            {
                throw new DbDriverException(Sts2ErrorCodes.QueryFailedServer, ex.Message,
                    new ServerErrorDetail { Number = ex.SqliteErrorCode, Severity = 16, State = 1 }, ex);
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
                        foreach (ExecEvent execEvent in await PumpResultSetAsync(reader, resultSetId, pageRows, linked.Token).ConfigureAwait(false))
                        {
                            yield return execEvent;
                        }
                        resultSetId++;
                    }
                    else
                    {
                        totalRowsAffected += reader.RecordsAffected >= 0 ? reader.RecordsAffected : 0;
                    }

                    hasResultSet = await reader.NextResultAsync(linked.Token).ConfigureAwait(false);
                }
                while (hasResultSet);

                yield return new ExecCompleted([reader.RecordsAffected >= 0 ? reader.RecordsAffected : totalRowsAffected]);
            }
        }

        /// <summary>
        /// Buffers one result set into result-set/page/completed events. Buffering (rather
        /// than yielding mid-read) keeps the SqliteException boundary outside the iterator,
        /// which C# forbids combining with yield.
        /// </summary>
        private static async Task<IReadOnlyList<ExecEvent>> PumpResultSetAsync(SqliteDataReader reader, int resultSetId, int pageRows, CancellationToken cancellationToken)
        {
            var events = new List<ExecEvent>();
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
            events.Add(new ResultSetStarted(resultSetId, columns));

            int pageSeq = 0;
            long rowOffset = 0;
            long rowCount = 0;
            var page = new List<IReadOnlyList<object?>>(pageRows);

            try
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var cells = new object?[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        cells[i] = EncodeCell(reader, i);
                    }
                    page.Add(cells);
                    rowCount++;

                    if (page.Count >= pageRows)
                    {
                        events.Add(new RowsPage(resultSetId, pageSeq, rowOffset, page));
                        rowOffset += page.Count;
                        pageSeq++;
                        page = new List<IReadOnlyList<object?>>(pageRows);
                    }
                }
            }
            catch (SqliteException ex)
            {
                throw new DbDriverException(Sts2ErrorCodes.QueryFailedServer, ex.Message,
                    new ServerErrorDetail { Number = ex.SqliteErrorCode, Severity = 16, State = 1 }, ex);
            }

            if (page.Count > 0)
            {
                events.Add(new RowsPage(resultSetId, pageSeq, rowOffset, page));
            }
            events.Add(new ResultSetCompleted(resultSetId, rowCount));
            return events;
        }

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
            // Cooperative cancellation: honored between pages and on the next read.
            activeQueryCancel.Cancel();
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }
            activeQueryCancel.Cancel();
            activeQueryCancel.Dispose();
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
