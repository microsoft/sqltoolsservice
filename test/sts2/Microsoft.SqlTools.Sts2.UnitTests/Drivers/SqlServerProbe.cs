//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    /// <summary>
    /// Probes for a reachable SQL Server (SPEC §14.5). The connection string comes from
    /// STS2_SQLSERVER_CONNSTRING. When unset or unreachable, engine tests skip with a
    /// reported reason rather than failing — CI/nightly sets the variable and must run them.
    /// </summary>
    internal static class SqlServerProbe
    {
        internal const string EnvVar = "STS2_SQLSERVER_CONNSTRING";

        private static readonly Lazy<(bool Available, string Reason)> Probe = new(Detect);

        internal static bool Available => Probe.Value.Available;

        internal static string SkipReason => Probe.Value.Reason;

        internal static string ConnectionString =>
            Environment.GetEnvironmentVariable(EnvVar)
            ?? throw new InvalidOperationException(EnvVar + " is not set");

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
                return (false, $"SQL Server not reachable via {EnvVar}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Minimal "skip, don't fail" fact: when SQL Server is unavailable the test method
    /// returns early after recording the reason via the output helper. Keeps the engine
    /// suite green locally (skipped) while CI exercises it. (A full Xunit.SkippableFact
    /// dependency is avoided to keep the dependency matrix lean.)
    /// </summary>
    internal static class EngineGate
    {
        internal static bool ShouldRun(Xunit.Abstractions.ITestOutputHelper output)
        {
            if (SqlServerProbe.Available)
            {
                return true;
            }
            output.WriteLine("ENGINE TEST SKIPPED — " + SqlServerProbe.SkipReason);
            return false;
        }
    }
}
