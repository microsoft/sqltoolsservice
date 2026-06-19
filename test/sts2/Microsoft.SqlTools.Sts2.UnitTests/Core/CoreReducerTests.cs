//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Text.Json;
using Microsoft.SqlTools.Sts2.Core;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Core
{
    /// <summary>SPEC §9.2: the reducer is pure, total, and deterministic.</summary>
    public class CoreReducerTests
    {
        private static CoreEnvelope Request(long seq, string type, string corr, string payloadJson = "{}") => new()
        {
            Seq = seq,
            Kind = "rpc.in.request",
            Type = type,
            Corr = corr,
            Payload = JsonDocument.Parse(payloadJson).RootElement.Clone(),
        };

        private static CoreEnvelope EffectResponse(long seq, string type, string corr, string payloadJson) => new()
        {
            Seq = seq,
            Kind = "effect.res",
            Type = type,
            Corr = corr,
            Payload = JsonDocument.Parse(payloadJson).RootElement.Clone(),
        };

        /// <summary>State with one open connection c-1 (handle h-1) ready for queries.</summary>
        private static CoreState OpenConnectionState()
        {
            CoreState state = CoreState.Initial;
            state = Sts2CoreReducer.Decide(state, Request(1, "v2/connection.open", "r-open",
                """{"openId":"o-1","profile":{"driver":"fake","server":"s"}}""")).NewState;
            return Sts2CoreReducer.Decide(state, EffectResponse(2, "driver.open", "drv-open-1",
                """{"connectionId":"c-1","openId":"o-1","status":"ok","handleId":"h-1","serverInfo":{"product":"Fake"}}""")).NewState;
        }

        [Fact]
        public void UnknownMethodYieldsStableErrorNotException()
        {
            CoreDecision decision = Sts2CoreReducer.Decide(CoreState.Initial, Request(1, "v2/nope", "r-9"));
            RpcErrorOutput error = Assert.IsType<RpcErrorOutput>(Assert.Single(decision.Outputs));
            Assert.Equal("Sts2.InvalidRequest", error.DataCode);
        }

        [Fact]
        public void QueryExecuteAcceptsAndGrantsInitialCredit()
        {
            CoreDecision decision = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}"""));

            Assert.Equal(2, decision.Outputs.Length);
            RpcResultOutput result = Assert.IsType<RpcResultOutput>(decision.Outputs[0]);
            Assert.Equal("q-3", result.Result.GetProperty("queryId").GetString());
            EffectRequestOutput start = Assert.IsType<EffectRequestOutput>(decision.Outputs[1]);
            Assert.Equal("driver.queryStart", start.EffectName);
            Assert.Equal(4, start.Args.GetProperty("credit").GetInt32()); // windowPages

            QueryInfo query = decision.NewState.Queries["q-3"];
            Assert.Equal(QueryPhase.Running, query.Phase);
            Assert.Equal(4, query.CreditOutstanding);
            Assert.Equal("q-3", decision.NewState.Connections["c-1"].ActiveQueryId);
        }

        [Fact]
        public void SecondQueryOnSameConnectionIsBusy()
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q1", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;
            CoreDecision second = Sts2CoreReducer.Decide(state,
                Request(4, "v2/query.execute", "r-q2", """{"connectionId":"c-1","sql":"select 2"}"""));

            RpcErrorOutput error = Assert.IsType<RpcErrorOutput>(Assert.Single(second.Outputs));
            Assert.Equal("Sts2.Busy", error.DataCode);
        }

        [Fact]
        public void RowsEventEmitsNotifyAndConsumesCredit()
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;
            CoreDecision decision = Sts2CoreReducer.Decide(state, EffectResponse(4, "driver.queryEvent", "evt-q-3",
                """{"queryId":"q-3","eventType":"rows","resultSetId":0,"pageSeq":0,"rowOffset":0,"rows":[[1,"a"]]}"""));

            RpcNotifyOutput notify = Assert.IsType<RpcNotifyOutput>(Assert.Single(decision.Outputs));
            Assert.Equal("v2/query.rows", notify.Method);
            Assert.Equal(0, notify.Params.GetProperty("pageSeq").GetInt32());
            Assert.Equal(1, decision.NewState.Queries["q-3"].PagesSent);
            Assert.Equal(3, decision.NewState.Queries["q-3"].CreditOutstanding);
        }

        [Fact]
        public void CompleteIsSentExactlyOnceAndOutputIsSuppressedAfter()
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;
            CoreDecision complete = Sts2CoreReducer.Decide(state, EffectResponse(4, "driver.queryEvent", "evt-q-3",
                """{"queryId":"q-3","eventType":"completed","rowsAffected":3}"""));

            RpcNotifyOutput notify = Assert.IsType<RpcNotifyOutput>(Assert.Single(complete.Outputs));
            Assert.Equal("v2/query.complete", notify.Method);
            Assert.Equal("succeeded", notify.Params.GetProperty("status").GetString());
            Assert.Null(complete.NewState.Connections["c-1"].ActiveQueryId);

            // I3: a straggler event after complete produces NO output.
            CoreDecision straggler = Sts2CoreReducer.Decide(complete.NewState, EffectResponse(5, "driver.queryEvent", "evt-q-3",
                """{"queryId":"q-3","eventType":"rows","pageSeq":1,"rows":[[2]]}"""));
            Assert.Empty(straggler.Outputs);
        }

        [Fact]
        public void AckGrantsCreditOnlyWhenWindowOpens()
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;

            // Simulate 4 pages sent (credit exhausted).
            for (int page = 0; page < 4; page++)
            {
                state = Sts2CoreReducer.Decide(state, EffectResponse(4 + page, "driver.queryEvent", "evt-q-3",
                    $$"""{"queryId":"q-3","eventType":"rows","pageSeq":{{page}},"rows":[[1]]}""")).NewState;
            }
            Assert.Equal(4, state.Queries["q-3"].PagesSent);
            Assert.Equal(0, state.Queries["q-3"].CreditOutstanding);

            // One ack opens one credit slot (I9: unacked never exceeds the window).
            CoreDecision ack = Sts2CoreReducer.Decide(state, new CoreEnvelope
            {
                Seq = 10,
                Kind = "rpc.in.notify",
                Type = "v2/query.ack",
                Payload = JsonDocument.Parse("""{"queryId":"q-3","pageSeq":0}""").RootElement.Clone(),
            });
            EffectRequestOutput advance = Assert.IsType<EffectRequestOutput>(Assert.Single(ack.Outputs));
            Assert.Equal("driver.queryAdvance", advance.EffectName);
            Assert.Equal(1, advance.Args.GetProperty("credit").GetInt32());

            // High-water ack through page 3 releases the rest.
            CoreDecision highWater = Sts2CoreReducer.Decide(ack.NewState, new CoreEnvelope
            {
                Seq = 11,
                Kind = "rpc.in.notify",
                Type = "v2/query.ack",
                Payload = JsonDocument.Parse("""{"queryId":"q-3","throughPageSeq":3}""").RootElement.Clone(),
            });
            EffectRequestOutput more = Assert.IsType<EffectRequestOutput>(Assert.Single(highWater.Outputs));
            Assert.Equal(3, more.Args.GetProperty("credit").GetInt32());
            Assert.Equal(4, highWater.NewState.Queries["q-3"].PagesAcked);
        }

        [Fact]
        public void CancelAndDisposeAreIdempotent()
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;

            CoreDecision cancel = Sts2CoreReducer.Decide(state, Request(4, "v2/query.cancel", "r-c1", """{"queryId":"q-3"}"""));
            Assert.Equal(2, cancel.Outputs.Length); // result {} + driver.queryCancel
            CoreDecision cancelAgain = Sts2CoreReducer.Decide(cancel.NewState, Request(5, "v2/query.cancel", "r-c2", """{"queryId":"q-3"}"""));
            Assert.IsType<RpcResultOutput>(Assert.Single(cancelAgain.Outputs)); // idempotent {}

            CoreDecision unknownCancel = Sts2CoreReducer.Decide(state, Request(6, "v2/query.cancel", "r-c3", """{"queryId":"q-nope"}"""));
            Assert.IsType<RpcResultOutput>(Assert.Single(unknownCancel.Outputs));

            CoreDecision dispose = Sts2CoreReducer.Decide(cancel.NewState, Request(7, "v2/query.dispose", "r-d1", """{"queryId":"q-3"}"""));
            Assert.Equal(2, dispose.Outputs.Length);
            CoreDecision disposeAgain = Sts2CoreReducer.Decide(dispose.NewState, Request(8, "v2/query.dispose", "r-d2", """{"queryId":"q-3"}"""));
            Assert.IsType<RpcResultOutput>(Assert.Single(disposeAgain.Outputs));
        }

        [Fact]
        public void CloseWithActiveQueryCancelsThenCloses()
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;

            CoreDecision close = Sts2CoreReducer.Decide(state, Request(4, "v2/connection.close", "r-close", """{"connectionId":"c-1"}"""));
            EffectRequestOutput queryCancel = Assert.IsType<EffectRequestOutput>(Assert.Single(close.Outputs));
            Assert.Equal("driver.queryCancel", queryCancel.EffectName); // no close result yet
            Assert.True(close.NewState.Connections["c-1"].CloseAfterQuery);

            // Query terminal -> complete notify + driver.close issued.
            CoreDecision terminal = Sts2CoreReducer.Decide(close.NewState, EffectResponse(5, "driver.queryEvent", "evt-q-3",
                """{"queryId":"q-3","eventType":"canceled"}"""));
            Assert.Equal(2, terminal.Outputs.Length);
            Assert.IsType<RpcNotifyOutput>(terminal.Outputs[0]);
            EffectRequestOutput closeEffect = Assert.IsType<EffectRequestOutput>(terminal.Outputs[1]);
            Assert.Equal("driver.close", closeEffect.EffectName);

            // Close effect resolves -> close result {}.
            CoreDecision closed = Sts2CoreReducer.Decide(terminal.NewState, EffectResponse(6, "driver.close", "drv-close-5",
                """{"connectionId":"c-1","status":"ok"}"""));
            RpcResultOutput closeResult = Assert.IsType<RpcResultOutput>(Assert.Single(closed.Outputs));
            Assert.Equal("r-close", closeResult.Corr);
            Assert.Empty(closed.NewState.Connections);
        }

        [Fact]
        public void CloseDuringOpenThatWinsTheRaceClosesNotOrphans()
        {
            // Regression for the simulator-found leak (seed 47): close arrives while the
            // connection is Opening (replies {} + cancelOpen). The open then WINS the
            // cancel race and reports ok. The connection must close, not become a
            // permanently-open orphan, and the driver.close must carry the handle.
            CoreState opening = Sts2CoreReducer.Decide(CoreState.Initial,
                Request(1, "v2/connection.open", "r-open", """{"openId":"o-1","profile":{"driver":"fake"}}""")).NewState;

            CoreDecision close = Sts2CoreReducer.Decide(opening,
                Request(2, "v2/connection.close", "r-close", """{"connectionId":"c-1"}"""));
            Assert.IsType<RpcResultOutput>(close.Outputs[0]); // {} now
            Assert.IsType<EffectRequestOutput>(close.Outputs[1]); // driver.cancelOpen
            Assert.True(close.NewState.Connections["c-1"].CloseAfterQuery);

            // Open wins the race: ok arrives after the close.
            CoreDecision openWon = Sts2CoreReducer.Decide(close.NewState, EffectResponse(3, "driver.open", "drv-open-1",
                """{"connectionId":"c-1","openId":"o-1","status":"ok","handleId":"h-1","serverInfo":{"product":"Fake"}}"""));
            Assert.IsType<RpcResultOutput>(openWon.Outputs[0]); // open request still succeeds
            EffectRequestOutput closeEffect = Assert.IsType<EffectRequestOutput>(openWon.Outputs[1]);
            Assert.Equal("driver.close", closeEffect.EffectName);
            Assert.Equal("h-1", closeEffect.Args.GetProperty("handleId").GetString());
            Assert.Equal(ConnectionPhase.Closing, openWon.NewState.Connections["c-1"].Phase);

            // driver.close resolves: connection gone, no spurious result (close already answered).
            CoreDecision closed = Sts2CoreReducer.Decide(openWon.NewState, EffectResponse(4, "driver.close", "drv-close-3",
                """{"connectionId":"c-1","status":"ok"}"""));
            Assert.Empty(closed.Outputs);
            Assert.Empty(closed.NewState.Connections);
        }

        [Fact]
        public void DecideIsDeterministic()
        {
            CoreEnvelope envelope = Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""");
            CoreState state = OpenConnectionState();
            CoreDecision a = Sts2CoreReducer.Decide(state, envelope);
            CoreDecision b = Sts2CoreReducer.Decide(state, envelope);
            // Record equality is reference-based for immutable maps; compare the
            // deterministic state dump and output payloads instead.
            Assert.Equal(
                Sts2.Runtime.Replay.JournalReplayer.DumpState(a.NewState, 3),
                Sts2.Runtime.Replay.JournalReplayer.DumpState(b.NewState, 3));
            Assert.Equal(
                ((RpcResultOutput)a.Outputs[0]).Result.GetRawText(),
                ((RpcResultOutput)b.Outputs[0]).Result.GetRawText());
        }

        [Theory]
        [InlineData("metric", "whatever")]
        [InlineData("effect.res", "driver.queryEvent")] // unknown query
        [InlineData("control", "lifecycle.bogus")]
        public void GarbageInputBecomesDiagnosticNeverThrows(string kind, string type)
        {
            CoreDecision decision = Sts2CoreReducer.Decide(CoreState.Initial, new CoreEnvelope
            {
                Seq = 1,
                Kind = kind,
                Type = type,
                Corr = "x",
            });
            DiagnosticOutput diag = Assert.IsType<DiagnosticOutput>(Assert.Single(decision.Outputs));
            Assert.Equal("core.unexpectedInput", diag.Name);
        }

        // ---- review hardening regressions ----

        private static CoreState QueryWithFourPagesSent()
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;
            for (int page = 0; page < 4; page++)
            {
                state = Sts2CoreReducer.Decide(state, EffectResponse(4 + page, "driver.queryEvent", "evt-q-3",
                    $$"""{"queryId":"q-3","eventType":"rows","pageSeq":{{page}},"rows":[[1]]}""")).NewState;
            }
            return state;
        }

        [Fact]
        public void FutureOrDuplicateAckCannotOverGrantCreditBeyondWindow() // R011
        {
            CoreState state = QueryWithFourPagesSent(); // 4 sent, 0 credit, 0 acked

            // A wildly-out-of-range high-water ack must not push pagesAcked past PagesSent,
            // so credit can never exceed the window (I9).
            CoreDecision ack = Sts2CoreReducer.Decide(state, Notify(10, "v2/query.ack",
                """{"queryId":"q-3","throughPageSeq":1000000}"""));
            EffectRequestOutput advance = Assert.IsType<EffectRequestOutput>(Assert.Single(ack.Outputs));
            Assert.Equal(4, advance.Args.GetProperty("credit").GetInt32()); // exactly the window, not more
            Assert.Equal(4, ack.NewState.Queries["q-3"].PagesAcked);        // clamped to PagesSent
            Assert.Equal(4, ack.NewState.Queries["q-3"].CreditOutstanding);

            // A duplicate of the same ack grants nothing further.
            CoreDecision again = Sts2CoreReducer.Decide(ack.NewState, Notify(11, "v2/query.ack",
                """{"queryId":"q-3","throughPageSeq":1000000}"""));
            Assert.Empty(again.Outputs);
        }

        [Theory]
        [InlineData("1.5")]
        [InlineData("1e999")]
        [InlineData("99999999999")]
        [InlineData("-5")]
        public void MalformedAckNumbersDoNotThrow(string throughValue) // R026
        {
            CoreState state = QueryWithFourPagesSent();
            // The reducer is total: a non-integral / out-of-range / negative number must not
            // fault the pump.
            CoreDecision decision = Sts2CoreReducer.Decide(state, Notify(10, "v2/query.ack",
                $$"""{"queryId":"q-3","throughPageSeq":{{throughValue}}}"""));
            // Either ignored or treated as per-page; never an exception, never over-window.
            Assert.True(decision.NewState.Queries["q-3"].CreditOutstanding <= Contracts.Sts2Defaults.WindowPages);
        }

        [Fact]
        public void DuplicateCloseWhileQueryActiveDoesNotOrphanFirstRequest() // R010
        {
            CoreState state = Sts2CoreReducer.Decide(OpenConnectionState(),
                Request(3, "v2/query.execute", "r-q", """{"connectionId":"c-1","sql":"select 1"}""")).NewState;

            // First close parks behind the active query (no result yet) and cancels it.
            CoreDecision close1 = Sts2CoreReducer.Decide(state, Request(4, "v2/connection.close", "r-close-1", """{"connectionId":"c-1"}"""));
            Assert.IsType<EffectRequestOutput>(Assert.Single(close1.Outputs)); // driver.queryCancel only
            Assert.Equal("r-close-1", close1.NewState.Connections["c-1"].CloseCorr);

            // Second close must be answered {} immediately and MUST NOT overwrite the first waiter.
            CoreDecision close2 = Sts2CoreReducer.Decide(close1.NewState, Request(5, "v2/connection.close", "r-close-2", """{"connectionId":"c-1"}"""));
            RpcResultOutput dup = Assert.IsType<RpcResultOutput>(Assert.Single(close2.Outputs));
            Assert.Equal("r-close-2", dup.Corr);
            Assert.Equal("r-close-1", close2.NewState.Connections["c-1"].CloseCorr); // first waiter preserved

            // Query terminal -> driver.close; close resolves -> the FIRST close finally gets its {}.
            CoreState afterCancel = Sts2CoreReducer.Decide(close2.NewState, EffectResponse(6, "driver.queryEvent", "evt-q-3",
                """{"queryId":"q-3","eventType":"canceled"}""")).NewState;
            CoreDecision closed = Sts2CoreReducer.Decide(afterCancel, EffectResponse(7, "driver.close", "drv-close-6",
                """{"connectionId":"c-1","status":"ok"}"""));
            RpcResultOutput closeResult = Assert.IsType<RpcResultOutput>(Assert.Single(closed.Outputs));
            Assert.Equal("r-close-1", closeResult.Corr); // exactly the original waiter, answered once
        }

        [Fact]
        public void DisposeOfActiveQueryEmitsExactlyOneTerminalAfterPumpStops() // D-0011 / R008
        {
            CoreState state = QueryWithFourPagesSent(); // running, mid-stream

            // Dispose answers {} and asks the runner to stop the pump, but emits NO terminal
            // yet and HOLDS the connection (ActiveQueryId stays set).
            CoreDecision dispose = Sts2CoreReducer.Decide(state, Request(20, "v2/query.dispose", "r-d", """{"queryId":"q-3"}"""));
            Assert.Equal(2, dispose.Outputs.Length);
            Assert.IsType<RpcResultOutput>(dispose.Outputs[0]);
            EffectRequestOutput disp = Assert.IsType<EffectRequestOutput>(dispose.Outputs[1]);
            Assert.Equal("driver.queryDispose", disp.EffectName);
            Assert.Equal(QueryPhase.Disposing, dispose.NewState.Queries["q-3"].Phase);
            Assert.Equal("q-3", dispose.NewState.Connections["c-1"].ActiveQueryId); // connection held

            // A driver event arriving during disposing is suppressed (no double terminal).
            CoreDecision straggler = Sts2CoreReducer.Decide(dispose.NewState, EffectResponse(21, "driver.queryEvent", "evt-q-3",
                """{"queryId":"q-3","eventType":"canceled"}"""));
            Assert.Empty(straggler.Outputs);

            // The runner confirms the pump stopped -> exactly one query.complete(disposed),
            // connection released.
            CoreDecision done = Sts2CoreReducer.Decide(straggler.NewState, EffectResponse(22, "driver.queryDispose", "drv-qdispose-20",
                """{"queryId":"q-3","status":"ok"}"""));
            RpcNotifyOutput complete = Assert.IsType<RpcNotifyOutput>(Assert.Single(done.Outputs));
            Assert.Equal("v2/query.complete", complete.Method);
            Assert.Equal("disposed", complete.Params.GetProperty("status").GetString());
            Assert.Equal(QueryPhase.Disposed, done.NewState.Queries["q-3"].Phase);
            Assert.Null(done.NewState.Connections["c-1"].ActiveQueryId);
        }

        [Fact]
        public void SetCaptureCannotExceedHostCapturePolicy() // D-0012
        {
            // session.start pins a product-style deny policy (digest ceiling).
            CoreState state = Sts2CoreReducer.Decide(CoreState.Initial, new CoreEnvelope
            {
                Seq = 1,
                Kind = "control",
                Type = "session.start",
                Payload = JsonDocument.Parse("""{"capture":{"row":"digest","sql":"digest","maxRow":"digest","maxSql":"digest"}}""").RootElement.Clone(),
            }).NewState;

            // A client may not elevate to full rows / text SQL beyond the policy.
            CoreDecision full = Sts2CoreReducer.Decide(state, Request(2, "v2/diagnostics.setCapture", "r-1", """{"rowCapture":"full"}"""));
            Assert.Equal("Sts2.InvalidRequest", Assert.IsType<RpcErrorOutput>(Assert.Single(full.Outputs)).DataCode);

            CoreDecision text = Sts2CoreReducer.Decide(state, Request(3, "v2/diagnostics.setCapture", "r-2", """{"sqlCapture":"text"}"""));
            Assert.Equal("Sts2.InvalidRequest", Assert.IsType<RpcErrorOutput>(Assert.Single(text.Outputs)).DataCode);

            // digest (the safe floor) is always permitted.
            CoreDecision digest = Sts2CoreReducer.Decide(state, Request(4, "v2/diagnostics.setCapture", "r-3", """{"rowCapture":"digest","sqlCapture":"digest"}"""));
            Assert.IsType<RpcResultOutput>(digest.Outputs[0]);
        }

        private static CoreEnvelope Notify(long seq, string type, string payloadJson) => new()
        {
            Seq = seq,
            Kind = "rpc.in.notify",
            Type = type,
            Payload = JsonDocument.Parse(payloadJson).RootElement.Clone(),
        };
    }
}
