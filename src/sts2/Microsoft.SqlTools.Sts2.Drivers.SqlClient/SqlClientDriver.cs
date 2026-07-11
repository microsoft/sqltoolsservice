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
            if (accessToken is not null)
            {
                connection.AccessToken = accessToken;
            }

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            catch (SqlException ex)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw new DbDriverException(
                    SqlClientErrorMapping.ClassifyOpen(ex),
                    "Connection failed: " + ex.Message,
                    SqlClientErrorMapping.ServerDetail(ex),
                    ex);
            }

            ServerInfo server = await ReadServerInfoAsync(connection, cancellationToken).ConfigureAwait(false);
            return new SqlClientSession(connection, server);
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
