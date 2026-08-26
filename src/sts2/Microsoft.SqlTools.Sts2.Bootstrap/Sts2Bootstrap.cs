//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
#if STS2_INCLUDE_SQLITE_DRIVER
using Microsoft.SqlTools.Sts2.Drivers.Sqlite;
#endif
using Microsoft.SqlTools.Sts2.Hosting;
using Microsoft.SqlTools.Sts2.Multiplexer;

namespace Microsoft.SqlTools.Sts2.Bootstrap
{
    /// <summary>
    /// STS2 composition root, called from the legacy <c>Program.Main</c> (the SPEC §5 seam).
    /// Owns activation flags: legacy argument parsing never sees <c>--enable-sts2</c>.
    /// </summary>
    public static class Sts2Bootstrap
    {
        private static readonly HashSet<string> CommandLineOptionsWithValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "--application-name",
            "--data-path",
            "--http-proxy-url",
            "--locale",
            "--log-file",
            "--parallel-message-processing-limit",
            "--parent-pid",
            "--service-name",
            "--tracing-level",
        };

        private static readonly HashSet<string> CommandLineSwitches = new(StringComparer.OrdinalIgnoreCase)
        {
            "-d",
            "-h",
            "--autoflush-log",
            "--developers",
            "--enable-connection-pooling",
            "--enable-logging",
            "--enable-sql-authentication-provider",
            "--enable-sts2",
            "--help",
            "--http-proxy-strict-ssl",
            "--parallel-message-processing",
            "--pii-logging",
            "--request-mfa-token-from-client",
            "--stdio",
            "--vscode-debug-launch",
        };

        /// <summary>Command-line flag that activates STS2 (SPEC §5.2).</summary>
        public const string EnableFlag = "--enable-sts2";

        /// <summary>Environment variable that activates STS2 when set to <c>1</c> (SPEC §5.2).</summary>
        public const string EnableEnvironmentVariable = "STS_ENABLE_STS2";

        /// <summary>Returns true when STS2 activation is requested (SPEC §5.2).</summary>
        public static bool IsEnabled(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            foreach (string arg in args)
            {
                if (string.Equals(arg, EnableFlag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return string.Equals(
                Environment.GetEnvironmentVariable(EnableEnvironmentVariable), "1", StringComparison.Ordinal);
        }

        /// <summary>
        /// Starts STS2 when enabled: takes ownership of the real console streams, starts
        /// the multiplexer and the STS2 RPC host, and returns the virtual legacy stream
        /// pair. When disabled, returns <see cref="Sts2BootstrapHandle.Disabled"/> and
        /// touches nothing: no multiplexer, no STS2 runtime, no files (SPEC §2.2, §5.3).
        /// </summary>
        public static Sts2BootstrapHandle TryStart(string[] args, string? logFilePath)
        {
            if (!IsEnabled(args))
            {
                return Sts2BootstrapHandle.Disabled;
            }

            // ServerChannel normally sets console encodings when it opens the console
            // streams itself; we open them here instead, so preserve that behavior.
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Stream realInput = Console.OpenStandardInput();
            Stream realOutput = Console.OpenStandardOutput();

            string logDirectory = ResolveLogDirectory(logFilePath);
            StreamWriter? diagnosticsLog = TryCreateDiagnosticsLog(logDirectory);
            var options = new MultiplexerOptions
            {
                DiagnosticListener = d => WriteDiagnostic(diagnosticsLog, d),
            };

            var multiplexer = new StdioMultiplexer(realInput, realOutput, options);

            string runId = string.Create(
                CultureInfo.InvariantCulture,
                $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}");
            Sts2Session session = Sts2Session.Start(
                new Sts2SessionOptions
                {
                    Input = multiplexer.Sts2Input,
                    Output = multiplexer.Sts2Output,
                    RunId = runId,
                    // One directory per run (R007): every process run journals into its own
                    // sts2/<runId>/ folder so replay, export, tail, and invariant checks can
                    // never conflate this run with an earlier one in the shared log directory.
                    JournalDirectory = Path.Combine(logDirectory, "sts2", runId),
                    ServiceVersion = typeof(Sts2Bootstrap).Assembly.GetName().Version?.ToString() ?? "0.0.0.0",
                    Drivers = CreateDrivers(),
                    CommandLine = SanitizeCommandLine(args),
                },
                multiplexer.Sts2OutputWriter,
                multiplexer.Sts2InputReader);

            // Crash containment (SPEC §6.5): if the STS2 host dies, mark the channel dead
            // so v2 requests get synthesized errors while legacy traffic continues.
            _ = session.Completion.ContinueWith(
                t => multiplexer.MarkSts2Dead(Sts2Session.RedactedFailureReason("STS2 host", t.Exception)),
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            multiplexer.Start(new SessionLifecycleSink(session, diagnosticsLog));
            return new Sts2BootstrapHandle(multiplexer.LegacyInput, multiplexer.LegacyOutput, multiplexer, session, diagnosticsLog);
        }

        private static Dictionary<string, IDbDriver> CreateDrivers()
        {
            var drivers = new Dictionary<string, IDbDriver>
            {
                ["sqlclient"] = new Drivers.SqlClient.SqlClientDriver(), // production (M5)
            };
#if STS2_INCLUDE_SQLITE_DRIVER
            // Non-production real-I/O test driver. The conditional project reference
            // keeps Microsoft.Data.Sqlite and every native SQLite RID out of release builds.
            drivers["sqlite"] = new SqliteDriver();
#endif
            return drivers;
        }

        /// <summary>
        /// Records the command line for the journal manifest by SHAPE, not by blocklist
        /// (R033): flag NAMES are kept verbatim (they aren't secret and are useful forensics),
        /// but every value/positional — which can carry a connection string, access token, or
        /// user-specific path — is redacted, including the inline value of <c>--flag=value</c>.
        /// The old substring-"password" filter missed tokens, connection strings, and
        /// next-argument value forms.
        /// </summary>
        internal static IReadOnlyList<string> SanitizeCommandLine(string[] args)
        {
            var result = new List<string>(args.Length);
            bool redactNextValue = false;
            foreach (string arg in args)
            {
                if (redactNextValue || string.IsNullOrEmpty(arg))
                {
                    result.Add("<redacted>");
                    redactNextValue = false;
                    continue;
                }

                int separator = arg.IndexOfAny(['=', ':']);
                string optionName = separator > 0 ? arg[..separator] : arg;
                bool optionHasValue = CommandLineOptionsWithValues.Contains(optionName);
                if (optionHasValue || CommandLineSwitches.Contains(optionName))
                {
                    result.Add(separator > 0
                        ? string.Concat(arg.AsSpan(0, separator + 1), "<redacted>")
                        : arg);
                    redactNextValue = optionHasValue && separator < 0;
                }
                else
                {
                    result.Add("<redacted>");
                }
            }
            return result;
        }

        internal static string ResolveLogDirectory(string? logFilePath)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                return Path.GetTempPath();
            }

            try
            {
                return Path.GetDirectoryName(Path.GetFullPath(logFilePath)) ?? Path.GetTempPath();
            }
            catch (Exception ex) when (ex is ArgumentException
                or IOException
                or NotSupportedException
                or UnauthorizedAccessException)
            {
                // A malformed optional legacy log path must not prevent STS2 startup.
                // Use the process temp directory for both diagnostics and journals.
                return Path.GetTempPath();
            }
        }

        private static StreamWriter? TryCreateDiagnosticsLog(string logDirectory)
        {
            try
            {
                string fileName = string.Create(
                    CultureInfo.InvariantCulture,
                    $"sts2-mux-{Environment.ProcessId}.log");
                return new StreamWriter(Path.Combine(logDirectory, fileName), append: false) { AutoFlush = true };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return null; // diagnostics must never block startup; stdout is off-limits (SPEC §6.6)
            }
        }

        private static void WriteDiagnostic(StreamWriter? log, MultiplexerDiagnostic diagnostic)
        {
            try
            {
                log?.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{DateTimeOffset.UtcNow:O} [{diagnostic.Code}] {diagnostic.Message}"));
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                // Never let diagnostics take the transport down.
            }
        }

        /// <summary>
        /// Journals the mirrored lifecycle signal and flushes before legacy can act on it
        /// (this repo's legacy host exits the process from shutdown, RF-0011).
        /// </summary>
        private sealed class SessionLifecycleSink : ISts2LifecycleSink
        {
            private readonly Sts2Session session;
            private readonly StreamWriter? diagnosticsLog;

            internal SessionLifecycleSink(Sts2Session session, StreamWriter? diagnosticsLog)
            {
                this.session = session;
                this.diagnosticsLog = diagnosticsLog;
            }

            public Task OnShutdownAsync() => SignalAsync("lifecycle.shutdown");

            public Task OnExitAsync() => SignalAsync("lifecycle.exit");

            private async Task SignalAsync(string signal)
            {
                try
                {
                    await session.SignalLifecycleAsync(signal).ConfigureAwait(false);
                    diagnosticsLog?.Flush();
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
                {
                    // Flush is best effort under a bounded wait; the mux forwards regardless.
                }
            }
        }
    }
}
