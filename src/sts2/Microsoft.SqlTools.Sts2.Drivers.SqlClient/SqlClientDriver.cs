//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Sts2.Abstractions;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>
    /// Production driver over Microsoft.Data.SqlClient (SPEC §10.2). Validates T-SQL
    /// semantics, SQL Server type metadata, and TDS error/cancellation behavior.
    /// </summary>
    public sealed class SqlClientDriver : IDbDriver
    {
        private readonly Func<SqlConnection, CancellationToken, Task> openConnectionAsync;
        private readonly Func<SqlConnection, CancellationToken, Task<ServerInfo>> readServerInfoAsync;
        private readonly Func<SqlConnection, ValueTask> disposeConnectionAsync;

        /// <summary>Creates the production SqlClient driver.</summary>
        public SqlClientDriver()
            : this(
                static (connection, cancellationToken) => connection.OpenAsync(cancellationToken),
                ReadServerInfoAsync,
                static connection => connection.DisposeAsync())
        {
        }

        /// <summary>Internal lifecycle seam for server-free failure-path tests.</summary>
        internal SqlClientDriver(
            Func<SqlConnection, CancellationToken, Task> openConnectionAsync,
            Func<SqlConnection, CancellationToken, Task<ServerInfo>> readServerInfoAsync,
            Func<SqlConnection, ValueTask> disposeConnectionAsync)
        {
            this.openConnectionAsync = openConnectionAsync ?? throw new ArgumentNullException(nameof(openConnectionAsync));
            this.readServerInfoAsync = readServerInfoAsync ?? throw new ArgumentNullException(nameof(readServerInfoAsync));
            this.disposeConnectionAsync = disposeConnectionAsync ?? throw new ArgumentNullException(nameof(disposeConnectionAsync));
        }

        /// <inheritdoc/>
        public string Name => "sqlclient";

        /// <inheritdoc/>
        public DriverCapabilities Capabilities { get; } = new() { Dialects = ["tsql"], Production = true };

        /// <inheritdoc/>
        public async ValueTask<IDbSession> OpenAsync(ConnectionOpenRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            (string connectionString, string? accessToken) = SqlClientConnectionString.Build(request);

            var connection = new SqlConnection(connectionString);

            try
            {
                if (accessToken is not null)
                {
                    connection.AccessToken = accessToken;
                }

                await openConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
                ServerInfo server = await readServerInfoAsync(connection, cancellationToken).ConfigureAwait(false);
                return new SqlClientSession(connection, server);
            }
            catch (SqlException ex)
            {
                await DisposeFailedConnectionAsync(connection).ConfigureAwait(false);
                throw new DbDriverException(
                    SqlClientErrorMapping.ClassifyOpen(ex),
                    "Connection failed: " + ex.Message,
                    SqlClientErrorMapping.ServerDetail(ex),
                    ex);
            }
            catch
            {
                // Ownership transfers to SqlClientSession only after server-info read.
                // Every failure before that point, including cancellation and probe
                // failures, must release the physical connection here.
                await DisposeFailedConnectionAsync(connection).ConfigureAwait(false);
                throw;
            }
        }

        private async ValueTask DisposeFailedConnectionAsync(SqlConnection connection)
        {
            try
            {
                await disposeConnectionAsync(connection).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Preserve the original open/probe failure; disposal is best-effort.
            }
        }

        private static async Task<ServerInfo> ReadServerInfoAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText =
                "select cast(serverproperty('ProductVersion') as nvarchar(128)), cast(serverproperty('Edition') as nvarchar(128)), cast(serverproperty('EngineEdition') as int)";
            try
            {
                await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return new ServerInfo
                    {
                        Product = "Microsoft SQL Server",
                        Version = reader.IsDBNull(0) ? connection.ServerVersion : reader.GetString(0),
                        EngineEdition = reader.IsDBNull(1) ? null : reader.GetString(1),
                        EngineEditionId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        Dialect = "tsql",
                    };
                }
            }
            catch (SqlException)
            {
                // Fall through to a minimal descriptor; server metadata is best-effort.
            }

            return new ServerInfo
            {
                Product = "Microsoft SQL Server",
                Version = connection.ServerVersion,
                EngineEdition = null,
                Dialect = "tsql",
            };
        }
    }
}
