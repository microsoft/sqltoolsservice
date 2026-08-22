//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>
    /// Probes for a reachable SQL Server (SPEC §14.5). The connection string comes from
    /// STS2_SQLSERVER_CONNSTRING. When unset or unreachable, engine tests skip with a
    /// reported reason rather than failing. CI/nightly sets both the connection string and
    /// STS2_REQUIRE_SQLSERVER so a missing or unreachable engine is a test failure, not a skip.
    /// </summary>
    internal static class SqlServerProbe
    {
        internal const string EnvVar = "STS2_SQLSERVER_CONNSTRING";
        internal const string RequireEnvVar = "STS2_REQUIRE_SQLSERVER";

        private static readonly Lazy<(bool Available, string Reason)> Probe = new(Detect);

        internal static bool Available => Probe.Value.Available;

        internal static string SkipReason => Probe.Value.Reason;

        internal static bool Required
        {
            get
            {
                string? value = Environment.GetEnvironmentVariable(RequireEnvVar);
                return string.Equals(value, "1", StringComparison.Ordinal)
                    || (bool.TryParse(value, out bool required) && required);
            }
        }

        internal static string ConnectionString
        {
            get
            {
                if (Required && !Available)
                {
                    throw new InvalidOperationException(
                        "SQL Server is required by " + RequireEnvVar + ", but " + SkipReason);
                }
                return Environment.GetEnvironmentVariable(EnvVar)
                    ?? throw new InvalidOperationException(EnvVar + " is not set");
            }
        }

        private static (bool, string) Detect()
        {
            string? connectionString = Environment.GetEnvironmentVariable(EnvVar);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return (false, $"{EnvVar} not set (no SQL Server configured; engine tests are CI/nightly).");
            }
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var connection = new SqlConnection(connectionString);
                connection.OpenAsync(cts.Token).GetAwaiter().GetResult();
                return (true, string.Empty);
            }
            catch (Exception ex) when (ex is SqlException or OperationCanceledException or InvalidOperationException)
            {
                return (false, UnavailableReason(ex));
            }
        }

        internal static string UnavailableReason(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            // Probe output is printed for skips and required-CI failures. Provider
            // messages may echo server/user/connection details, so retain only the
            // exception classification needed to distinguish timeout/config failures.
            return $"SQL Server not reachable via {EnvVar} ({exception.GetType().Name}).";
        }
    }

    /// <summary>Discovery-time skip for optional real-engine tests.</summary>
    internal sealed class EngineFactAttribute : FactAttribute
    {
        public EngineFactAttribute()
        {
            if (!SqlServerProbe.Available && !SqlServerProbe.Required)
            {
                Skip = SqlServerProbe.SkipReason;
            }
        }
    }
}
