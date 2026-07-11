//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>Builds SqlClient connection strings from sanitized profiles (server-free, unit-testable).</summary>
    public static class SqlClientConnectionString
    {
        /// <summary>Builds the connection string and returns the optional access token to attach.</summary>
        public static (string ConnectionString, string? AccessToken) Build(ConnectionOpenRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = request.Server,
                InitialCatalog = request.Database ?? string.Empty,
                ApplicationName = request.ApplicationName ?? "sts2",
                ConnectTimeout = request.ConnectTimeoutMs > 0
                    ? Math.Max(1, request.ConnectTimeoutMs / 1000)
                    : Math.Max(1, Sts2Defaults.ConnectTimeoutMs / 1000),
            };

            ApplyOptions(builder, request);

            string? accessToken = null;
            switch (request.Auth.Kind)
            {
                case "sqlLogin":
                    builder.UserID = request.Auth.User ?? string.Empty;
                    builder.Password = request.Auth.Secret ?? string.Empty;
                    break;
                case "accessToken":
                    // Static access tokens must not participate in SqlClient pools: a
                    // physical connection can otherwise be reused after the token's
                    // authentication context has expired or changed.
                    builder.Pooling = false;
                    accessToken = request.Auth.Secret; // attached to SqlConnection.AccessToken
                    break;
                case "integrated":
                    builder.IntegratedSecurity = true;
                    break;
                default:
                    throw new DbDriverException(Sts2ErrorCodes.InvalidRequest, "Unsupported auth kind: " + request.Auth.Kind);
            }

            return (builder.ConnectionString, accessToken);
        }

        private static void ApplyOptions(SqlConnectionStringBuilder builder, ConnectionOpenRequest request)
        {
            if (request.Options.TryGetValue("encrypt", out string? encrypt))
            {
                builder.Encrypt = encrypt switch
                {
                    "strict" => SqlConnectionEncryptOption.Strict,
                    "true" or "mandatory" => SqlConnectionEncryptOption.Mandatory,
                    "false" or "optional" => SqlConnectionEncryptOption.Optional,
                    _ => builder.Encrypt,
                };
            }
            if (request.Options.TryGetValue("trustServerCertificate", out string? trust))
            {
                builder.TrustServerCertificate = string.Equals(trust, "true", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
