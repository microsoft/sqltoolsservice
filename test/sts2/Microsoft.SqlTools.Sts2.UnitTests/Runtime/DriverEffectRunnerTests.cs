//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Runtime.Coordination;
using Microsoft.SqlTools.Sts2.Runtime.Effects;
using Microsoft.SqlTools.Sts2.Runtime.Redaction;
using Microsoft.SqlTools.Sts2.Testing;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Runtime
{
    public sealed class DriverEffectRunnerTests
    {
        [Fact]
        public async Task TeardownAwaitsAndReleasesOpenAndQueryWork()
        {
            var driver = new FakeDriver();
            var inbox = new RecordingInbox();
            var runner = Runner(driver);

            driver.EnqueueOpen(new FakeOpenBehavior { Outcome = "hang" });
            runner.Run(Effect(1, "open-hang", "driver.open",
                """{"connectionId":"c-1","openId":"hang","profile":{"driver":"fake","server":"s"}}"""), inbox);
            await WaitUntilAsync(() => runner.OpensInFlightCount == 1);
            await runner.DisposeLeakedSessionsAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));

            Assert.Equal(0, runner.OpensInFlightCount);
            Assert.Equal(0, runner.ActiveQueryPumpCount);
            Assert.Equal(0, runner.OpenSessionCount);
            Assert.Equal(0, runner.BackgroundTaskCount);

            // A second runner proves the query-pump path, including its CTS/semaphore
            // finally cleanup, independently from the canceled open above.
            driver = new FakeDriver();
            inbox = new RecordingInbox();
            runner = Runner(driver);
            runner.Run(Effect(1, "open-ok", "driver.open",
                """{"connectionId":"c-1","openId":"ok","profile":{"driver":"fake","server":"s"}}"""), inbox);
            JsonElement opened = await inbox.WaitForAsync("driver.open");
            string handleId = opened.GetProperty("handleId").GetString()!;
            driver.EnqueueQuery(new FakeQueryScript
            {
                Steps =
                [
                    new FakeQueryStep { Type = "resultSet", ResultSetId = 0, Columns = 1 },
                    new FakeQueryStep { Type = "hang" },
                ],
            });
            runner.Run(Effect(2, "query", "driver.queryStart",
                $$"""{"queryId":"q-2","handleId":"{{handleId}}","sql":"select forever","credit":4}"""), inbox);
            await WaitUntilAsync(() => runner.ActiveQueryPumpCount == 1);

            int leaked = await runner.DisposeLeakedSessionsAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            await WaitUntilAsync(() => runner.BackgroundTaskCount == 0);
            Assert.Equal(1, leaked);
            Assert.Equal(0, runner.OpensInFlightCount);
            Assert.Equal(0, runner.ActiveQueryPumpCount);
            Assert.Equal(0, runner.OpenSessionCount);
            Assert.Equal(0, driver.OpenSessionCount);
        }

        [Fact]
        public async Task ArbitraryCloseFailureStillPostsOneRedactedCompletion()
        {
            const string sensitiveMessage = "provider detail MUST NOT JOURNAL";
            var driver = new FakeDriver
            {
                SessionDisposeException = new InvalidOperationException(sensitiveMessage),
            };
            var inbox = new RecordingInbox();
            var runner = Runner(driver);
            runner.Run(Effect(1, "open", "driver.open",
                """{"connectionId":"c-1","openId":"ok","profile":{"driver":"fake","server":"s"}}"""), inbox);
            JsonElement opened = await inbox.WaitForAsync("driver.open");

            runner.Run(Effect(2, "close", "driver.close",
                $$"""{"connectionId":"c-1","handleId":"{{opened.GetProperty("handleId").GetString()}}"}"""), inbox);
            JsonElement closed = await inbox.WaitForAsync("driver.close");

            Assert.Equal("ok", closed.GetProperty("status").GetString());
            Assert.Equal(nameof(InvalidOperationException), closed.GetProperty("cleanupError").GetString());
            Assert.DoesNotContain(sensitiveMessage, closed.GetRawText(), StringComparison.Ordinal);
            Assert.Equal(1, inbox.Responses.Count(response => response.EffectName == "driver.close"));
            Assert.Equal(0, runner.OpenSessionCount);
            await runner.DisposeAsync();
        }

        [Fact]
        public async Task RejectedInitialQueryPostStillReleasesPumpOwnership()
        {
            var driver = new FakeDriver();
            var inbox = new RecordingInbox();
            var runner = Runner(driver);
            runner.Run(Effect(1, "open", "driver.open",
                """{"connectionId":"c-1","openId":"ok","profile":{"driver":"fake","server":"s"}}"""), inbox);
            JsonElement opened = await inbox.WaitForAsync("driver.open");
            inbox.RejectResponses = true;

            runner.Run(Effect(2, "query", "driver.queryStart",
                $$"""{"queryId":"q-rejected","handleId":"{{opened.GetProperty("handleId").GetString()}}","sql":"select 1","credit":1}"""), inbox);
            await WaitUntilAsync(() => runner.BackgroundTaskCount == 0);

            Assert.Equal(0, runner.ActiveQueryPumpCount);
            Assert.Equal(1, await runner.DisposeLeakedSessionsAsync());
            Assert.Equal(0, driver.OpenSessionCount);
        }

        private static DriverEffectRunner Runner(FakeDriver driver) => new(
            new Dictionary<string, IDbDriver> { ["fake"] = driver },
            new SecretSideTable());

        private static EffectWorkItem Effect(long cause, string id, string name, string json) => new()
        {
            CauseSeq = cause,
            EffectId = id,
            EffectName = name,
            Args = JsonDocument.Parse(json).RootElement.Clone(),
        };

        private static async Task WaitUntilAsync(Func<bool> predicate)
        {
            for (int i = 0; i < 200; i++)
            {
                if (predicate())
                {
                    return;
                }
                await Task.Delay(10);
            }
            throw new TimeoutException("Condition was not reached.");
        }

        private sealed class RecordingInbox : ICoordinatorInbox
        {
            private readonly SemaphoreSlim available = new(0);

            internal ConcurrentQueue<Response> Responses { get; } = new();

            internal bool RejectResponses { get; set; }

            public ValueTask PostEffectResponseAsync(
                string effectId,
                string effectName,
                JsonElement? payload,
                long causeSeq)
            {
                if (RejectResponses)
                {
                    return new ValueTask(Task.FromException(
                        new InvalidOperationException("coordinator inbox is closed")));
                }
                Responses.Enqueue(new Response(effectId, effectName, payload?.Clone(), causeSeq));
                available.Release();
                return ValueTask.CompletedTask;
            }

            internal async Task<JsonElement> WaitForAsync(string effectName)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                while (true)
                {
                    await available.WaitAsync(timeout.Token);
                    Response? response = Responses.FirstOrDefault(candidate => candidate.EffectName == effectName);
                    if (response is not null)
                    {
                        return response.Payload!.Value;
                    }
                }
            }
        }

        private sealed record Response(string EffectId, string EffectName, JsonElement? Payload, long CauseSeq);
    }
}
