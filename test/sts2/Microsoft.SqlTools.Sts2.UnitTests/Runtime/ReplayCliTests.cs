//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using ReplayProgram = Microsoft.SqlTools.Sts2.Replay.Program;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    [CollectionDefinition("Replay CLI", DisableParallelization = true)]
    public sealed class ReplayCliCollectionDefinition
    {
    }

    [Collection("Replay CLI")]
    public sealed class ReplayCliTests : IDisposable
    {
        private readonly string directory = Path.Combine(
            Path.GetTempPath(), "sts2-replay-cli-test-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        [Fact]
        public async Task IncompleteJournalIsReportedByRunVerifyAndDiff()
        {
            Directory.CreateDirectory(directory);
            JsonElement payload = JsonDocument.Parse("""{"echo":"unfinished"}""").RootElement.Clone();
            await using (var writer = new JournalWriter(
                "cli-incomplete",
                new JournalOptions { Directory = directory },
                new JournalRunInfo { ServiceVersion = "test" }))
            {
                await writer.AppendAsync(new Sts2Envelope
                {
                    RunId = "cli-incomplete",
                    Seq = 1,
                    Ts = DateTimeOffset.UnixEpoch,
                    Kind = EnvelopeKinds.RpcInRequest,
                    Type = "v2/diagnostics.ping",
                    Corr = "request-1",
                    ConfigVersion = 1,
                    Digest = CanonicalJson.DigestOf(payload),
                    Payload = payload,
                }, flush: true);
            }

            (int runExit, _, string runError) = Invoke("run", directory);
            Assert.Equal(1, runExit);
            Assert.Contains("INCOMPLETE after seq 1: 1 pending output(s)", runError);

            (int verifyExit, _, string verifyError) = Invoke("verify", directory);
            Assert.Equal(1, verifyExit);
            Assert.Contains("INCOMPLETE after seq 1: 1 pending output(s)", verifyError);

            (int diffExit, string diffOutput, string diffError) = Invoke("diff", directory);
            Assert.Equal(1, diffExit);
            Assert.Contains("incomplete journal after seq 1: 1 pending output(s)", diffOutput);
            Assert.Empty(diffError);
        }

        private static (int ExitCode, string Output, string Error) Invoke(params string[] args)
        {
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;
            using var output = new StringWriter();
            using var error = new StringWriter();
            try
            {
                Console.SetOut(output);
                Console.SetError(error);
                int exitCode = ReplayProgram.Main(args);
                return (exitCode, output.ToString(), error.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}
