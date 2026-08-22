//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Hosting;
using Microsoft.SqlTools.Sts2.Runtime.Envelopes;
using Microsoft.SqlTools.Sts2.Runtime.Journaling;
using Microsoft.SqlTools.Sts2.Testing;
using Microsoft.SqlTools.Sts2.UnitTests.Multiplexer;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Bootstrap
{
    public sealed class Sts2SessionStartupTests
    {
        [Fact]
        public async Task BufferedRpcCannotOvertakeCommittedPrivacyPolicy()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "sts2-startup-order-" + Guid.NewGuid().ToString("N"));
            const string SqlCanary = "private-sql-before-listener";
            var input = new Pipe();
            var output = new Pipe();
            await input.Writer.WriteAsync(Frames.Frame(
                """{"jsonrpc":"2.0","id":"init","method":"v2/initialize","params":{"clientName":"buffered-test"}}"""));
            await input.Writer.WriteAsync(Frames.Frame(
                """{"jsonrpc":"2.0","id":"query","method":"v2/query.execute","params":{"connectionId":"missing","sql":"private-sql-before-listener"}}"""));

            Sts2Session? session = null;
            try
            {
                session = Sts2Session.Start(
                    new Sts2SessionOptions
                    {
                        Input = Stream.Null,
                        Output = Stream.Null,
                        RunId = "startup-order",
                        JournalDirectory = directory,
                        ServiceVersion = "startup-test",
                        Drivers = new Dictionary<string, IDbDriver> { ["fake"] = new FakeDriver() },
                        CommandLine = ["--enable-sts2"],
                    },
                    output.Writer,
                    input.Reader);

                using Stream responseStream = output.Reader.AsStream(leaveOpen: true);
                JsonElement? initialize = null;
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                for (int i = 0; i < 2; i++)
                {
                    using JsonDocument response = JsonDocument.Parse(
                        await Frames.ReadFrameAsync(responseStream, timeout.Token));
                    if (response.RootElement.GetProperty("id").GetString() == "init")
                    {
                        initialize = response.RootElement.Clone();
                    }
                }

                JsonElement initializeResult = Assert.NotNull(initialize).GetProperty("result");
                Assert.Equal("digest", initializeResult.GetProperty("journal").GetProperty("capture").GetString());
                Assert.Equal("digest", initializeResult.GetProperty("journal").GetProperty("maxCapture").GetString());

                await session.Coordinator.PostControlBarrierAsync("test.buffered-rpc-observed");
                List<Sts2Envelope> envelopes = JournalReader.ReadAll(directory).ToList();
                Assert.NotEmpty(envelopes);
                Assert.Equal(1, envelopes[0].Seq);
                Assert.Equal("session.start", envelopes[0].Type);
                string journalText = JsonSerializer.Serialize(envelopes);
                Assert.DoesNotContain(SqlCanary, journalText, StringComparison.Ordinal);
            }
            finally
            {
                if (session is not null)
                {
                    await session.DisposeAsync();
                }
            }

            try
            {
                string manifestPath = Assert.Single(
                    Directory.EnumerateFiles(directory, "journal-*.manifest.json"));
                JournalManifest manifest = JournalReader.ReadManifest(manifestPath);
                Assert.Equal("digest", manifest.EffectiveConfiguration["capture.row"]);
                Assert.Equal("pipe", manifest.EffectiveConfiguration["transport.endpoint"]);
                Assert.False(string.IsNullOrWhiteSpace(manifest.DriverPackageVersions["fake"]));
            }
            finally
            {
                try
                {
                    Directory.Delete(directory, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
