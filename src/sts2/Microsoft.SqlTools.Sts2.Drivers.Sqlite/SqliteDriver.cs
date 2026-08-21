//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Drivers.Sqlite
{
    /// <summary>
    /// Portable real-I/O driver over Microsoft.Data.Sqlite (SPEC §10.3). Proves the
    /// driver port is honest against real async/file behavior without SQL Server
    /// infrastructure. Not a production driver: it does not validate T-SQL semantics.
    /// </summary>
    public sealed class SqliteDriver : IDbDriver
    {
        /// <inheritdoc/>
        public string Name => "sqlite";

        /// <inheritdoc/>
        public DriverCapabilities Capabilities { get; } = new() { Dialects = ["sqlite", "neutral"], Production = false };

        /// <inheritdoc/>
        public async ValueTask<IDbSession> OpenAsync(ConnectionOpenRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            SqliteConnection? connection = null;
            try
            {
                var builder = new SqliteConnectionStringBuilder
                {
                    DataSource = string.IsNullOrEmpty(request.Server) ? ":memory:" : request.Server,
                };
                if (request.Options.TryGetValue("mode", out string? mode) && Enum.TryParse(mode, ignoreCase: true, out SqliteOpenMode openMode))
                {
                    builder.Mode = openMode;
                }
                if (request.Options.TryGetValue("cache", out string? cache) && Enum.TryParse(cache, ignoreCase: true, out SqliteCacheMode cacheMode))
                {
                    builder.Cache = cacheMode;
                }

                connection = new SqliteConnection(builder.ToString());
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                var server = new ServerInfo
                {
                    Product = "SQLite",
                    Version = connection.ServerVersion,
                    EngineEdition = "Embedded",
                    Dialect = "sqlite",
                };
                return new SqliteSession(connection, server);
            }
            catch (OperationCanceledException)
            {
                if (connection is not null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                throw;
            }
            catch (SqliteException ex)
            {
                if (connection is not null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                throw new DbDriverException(ClassifyOpen(ex), "Failed to open Sqlite database: " + ex.Message,
                    new ServerErrorDetail { Number = ex.SqliteErrorCode, Severity = 16, State = 1 });
            }
            catch (Exception)
            {
                if (connection is not null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
                throw new DbDriverException(
                    Sts2ErrorCodes.ConnectionFailedNetwork,
                    "Failed to open Sqlite database.");
            }
        }

        private static string ClassifyOpen(SqliteException ex) => ex.SqliteErrorCode switch
        {
            14 => Sts2ErrorCodes.ConnectionFailedNetwork, // SQLITE_CANTOPEN
            _ => Sts2ErrorCodes.ConnectionFailedNetwork,
        };
    }
}
