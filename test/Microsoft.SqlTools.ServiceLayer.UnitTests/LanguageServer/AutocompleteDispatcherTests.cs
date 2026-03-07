//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    public class AutocompleteDispatcherTests : LanguageServiceTestBase<CompletionItem>
    {
        private static readonly RequestType<int, int> DispatcherProbeRequest =
            RequestType<int, int>.Create("test/dispatcherProbe");

        protected override LanguageService CreateLanguageService()
        {
            return new BlockingLanguageService();
        }

        [Test]
        public void HandleCompletionResolveRequest_StalledInnerWork_DoesNotBlockDispatcherQueue()
        {
            InitializeTestObjects();

            var blockingLanguageService = (BlockingLanguageService)langService;
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = true;
            blockingLanguageService.ResolveCompletionItemCallback = completionItem =>
            {
                blockingLanguageService.ResolveCompletionStarted.Set();
                Assert.True(
                    blockingLanguageService.AllowResolveCompletionToContinue.Wait(TaskTimeout),
                    "Expected completion resolve test to release the blocked inner operation");
                blockingLanguageService.ResolveCompletionCompleted.Set();
                return completionItem;
            };

            var dispatcher = CreateDispatcher();
            dispatcher.SetRequestHandler(CompletionResolveRequest.Type, blockingLanguageService.HandleCompletionResolveRequest);

            var dispatcherProbeHandled = new ManualResetEventSlim(initialState: false);
            dispatcher.SetRequestHandler(
                DispatcherProbeRequest,
                async (value, requestContext) =>
                {
                    dispatcherProbeHandled.Set();
                    await requestContext.SendResult(value);
                });

            var completionItem = new CompletionItem { Label = "select" };
            var stalledDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "1",
                    method: CompletionResolveRequest.Type.MethodName,
                    contents: JToken.FromObject(completionItem)));

            Assert.True(
                stalledDispatchTask.Wait(1000),
                "Expected completion resolve handler dispatch to return without waiting for the inner operation");
            Assert.True(
                blockingLanguageService.ResolveCompletionStarted.Wait(TaskTimeout),
                "Expected stalled completion resolve work to start in the background");
            Assert.False(
                blockingLanguageService.ResolveCompletionCompleted.IsSet,
                "Expected completion resolve work to remain stalled before releasing it");

            var followUpDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "2",
                    method: DispatcherProbeRequest.MethodName,
                    contents: JToken.FromObject(1)));

            Assert.True(
                followUpDispatchTask.Wait(1000),
                "Expected the dispatcher to process a second request while completion resolve work is stalled");
            Assert.True(
                dispatcherProbeHandled.Wait(1000),
                "Expected the follow-up dispatcher request to be handled while completion resolve work is stalled");

            blockingLanguageService.AllowResolveCompletionToContinue.Set();
            Assert.True(
                blockingLanguageService.ResolveCompletionCompleted.Wait(TaskTimeout),
                "Expected completion resolve work to finish after being released");
        }

        [Test]
        public void HandleHoverRequest_StalledInnerWork_DoesNotBlockDispatcherQueue()
        {
            InitializeTestObjects();

            var blockingLanguageService = (BlockingLanguageService)langService;
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableQuickInfo = true;
            blockingLanguageService.GetHoverItemCallback = (textDocumentPosition, scriptFile) =>
            {
                blockingLanguageService.GetHoverItemStarted.Set();
                Assert.True(
                    blockingLanguageService.AllowGetHoverItemToContinue.Wait(TaskTimeout),
                    "Expected hover test to release the blocked inner operation");
                blockingLanguageService.GetHoverItemCompleted.Set();
                return new Hover
                {
                    Contents = new[]
                    {
                        new MarkedString
                        {
                            Language = LanguageService.SQL_LANG,
                            Value = "hover"
                        }
                    }
                };
            };

            var dispatcher = CreateDispatcher();
            MethodInfo handleHoverRequestMethod = typeof(LanguageService).GetMethod(
                "HandleHoverRequest",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(handleHoverRequestMethod);
            dispatcher.ParallelMessageProcessing = true;
            dispatcher.SetRequestHandler(
                HoverRequest.Type,
                (textDocumentPosition, requestContext) =>
                {
                    return (Task)handleHoverRequestMethod.Invoke(
                        blockingLanguageService,
                        new object[] { textDocumentPosition, requestContext });
                },
                overrideExisting: false,
                isParallelProcessingSupported: true);
            Assert.True(dispatcher.requestHandlerParallelismMap[HoverRequest.Type.MethodName]);

            var dispatcherProbeHandled = new ManualResetEventSlim(initialState: false);
            dispatcher.SetRequestHandler(
                DispatcherProbeRequest,
                async (value, requestContext) =>
                {
                    dispatcherProbeHandled.Set();
                    await requestContext.SendResult(value);
                });

            var stalledDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "1",
                    method: HoverRequest.Type.MethodName,
                    contents: JToken.FromObject(textDocument)));

            Assert.True(
                stalledDispatchTask.Wait(1000),
                "Expected hover handler dispatch to return without waiting for the inner operation");
            Assert.True(
                blockingLanguageService.GetHoverItemStarted.Wait(TaskTimeout),
                "Expected stalled hover work to start in the background");
            Assert.False(
                blockingLanguageService.GetHoverItemCompleted.IsSet,
                "Expected hover work to remain stalled before releasing it");

            var followUpDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "2",
                    method: DispatcherProbeRequest.MethodName,
                    contents: JToken.FromObject(1)));

            Assert.True(
                followUpDispatchTask.Wait(1000),
                "Expected the dispatcher to process a second request while hover work is stalled");
            Assert.True(
                dispatcherProbeHandled.Wait(1000),
                "Expected the follow-up dispatcher request to be handled while hover work is stalled");

            blockingLanguageService.AllowGetHoverItemToContinue.Set();
            Assert.True(
                blockingLanguageService.GetHoverItemCompleted.Wait(TaskTimeout),
                "Expected hover work to finish after being released");
        }

        [Test]
        public void HandleSignatureHelpRequest_StalledInnerWork_DoesNotBlockDispatcherQueue()
        {
            InitializeTestObjects();

            var blockingLanguageService = (BlockingLanguageService)langService;
            blockingLanguageService.GetSignatureHelpCallback = async (textDocumentPosition, scriptFile) =>
            {
                blockingLanguageService.GetSignatureHelpStarted.Set();
                Assert.True(
                    blockingLanguageService.AllowGetSignatureHelpToContinue.Wait(TaskTimeout),
                    "Expected signature help test to release the blocked inner operation");
                blockingLanguageService.GetSignatureHelpCompleted.Set();
                return await Task.FromResult(new SignatureHelp());
            };

            var dispatcher = CreateDispatcher();
            dispatcher.ParallelMessageProcessing = true;
            dispatcher.SetRequestHandler(
                SignatureHelpRequest.Type,
                blockingLanguageService.HandleSignatureHelpRequest,
                overrideExisting: false,
                isParallelProcessingSupported: true);
            Assert.True(dispatcher.requestHandlerParallelismMap[SignatureHelpRequest.Type.MethodName]);

            var dispatcherProbeHandled = new ManualResetEventSlim(initialState: false);
            dispatcher.SetRequestHandler(
                DispatcherProbeRequest,
                async (value, requestContext) =>
                {
                    dispatcherProbeHandled.Set();
                    await requestContext.SendResult(value);
                });

            var stalledDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "1",
                    method: SignatureHelpRequest.Type.MethodName,
                    contents: JToken.FromObject(textDocument)));

            Assert.True(
                stalledDispatchTask.Wait(1000),
                "Expected signature help handler dispatch to return without waiting for the inner operation");
            Assert.True(
                blockingLanguageService.GetSignatureHelpStarted.Wait(TaskTimeout),
                "Expected stalled signature help work to start in the background");
            Assert.False(
                blockingLanguageService.GetSignatureHelpCompleted.IsSet,
                "Expected signature help work to remain stalled before releasing it");

            var followUpDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "2",
                    method: DispatcherProbeRequest.MethodName,
                    contents: JToken.FromObject(1)));

            Assert.True(
                followUpDispatchTask.Wait(1000),
                "Expected the dispatcher to process a second request while signature help work is stalled");
            Assert.True(
                dispatcherProbeHandled.Wait(1000),
                "Expected the follow-up dispatcher request to be handled while signature help work is stalled");

            blockingLanguageService.AllowGetSignatureHelpToContinue.Set();
            Assert.True(
                blockingLanguageService.GetSignatureHelpCompleted.Wait(TaskTimeout),
                "Expected signature help work to finish after being released");
        }

        [Test]
        public void HandleDefinitionRequest_StalledInnerWork_DoesNotBlockDispatcherQueue()
        {
            InitializeTestObjects();

            var blockingLanguageService = (BlockingLanguageService)langService;
            blockingLanguageService.GetDefinitionCallback = (textDocumentPosition, scriptFile, connInfo) =>
            {
                blockingLanguageService.GetDefinitionStarted.Set();
                Assert.True(
                    blockingLanguageService.AllowGetDefinitionToContinue.Wait(TaskTimeout),
                    "Expected definition test to release the blocked inner operation");
                blockingLanguageService.GetDefinitionCompleted.Set();
                return new DefinitionResult
                {
                    Locations = Array.Empty<Location>()
                };
            };

            var dispatcher = CreateDispatcher();
            dispatcher.ParallelMessageProcessing = true;
            dispatcher.SetRequestHandler(
                DefinitionRequest.Type,
                blockingLanguageService.HandleDefinitionRequest,
                overrideExisting: false,
                isParallelProcessingSupported: true);
            Assert.True(dispatcher.requestHandlerParallelismMap[DefinitionRequest.Type.MethodName]);

            var dispatcherProbeHandled = new ManualResetEventSlim(initialState: false);
            dispatcher.SetRequestHandler(
                DispatcherProbeRequest,
                async (value, requestContext) =>
                {
                    dispatcherProbeHandled.Set();
                    await requestContext.SendResult(value);
                });

            var stalledDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "1",
                    method: DefinitionRequest.Type.MethodName,
                    contents: JToken.FromObject(textDocument)));

            Assert.True(
                stalledDispatchTask.Wait(1000),
                "Expected definition handler dispatch to return without waiting for the inner operation");
            Assert.True(
                blockingLanguageService.GetDefinitionStarted.Wait(TaskTimeout),
                "Expected stalled definition work to start in the background");
            Assert.False(
                blockingLanguageService.GetDefinitionCompleted.IsSet,
                "Expected definition work to remain stalled before releasing it");

            var followUpDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "2",
                    method: DispatcherProbeRequest.MethodName,
                    contents: JToken.FromObject(1)));

            Assert.True(
                followUpDispatchTask.Wait(1000),
                "Expected the dispatcher to process a second request while definition work is stalled");
            Assert.True(
                dispatcherProbeHandled.Wait(1000),
                "Expected the follow-up dispatcher request to be handled while definition work is stalled");

            blockingLanguageService.AllowGetDefinitionToContinue.Set();
            Assert.True(
                blockingLanguageService.GetDefinitionCompleted.Wait(TaskTimeout),
                "Expected definition work to finish after being released");
        }

        [Test]
        public void HandleCompletionRequest_StalledInnerWork_DoesNotBlockDispatcherQueue()
        {
            InitializeTestObjects();

            var blockingLanguageService = (BlockingLanguageService)langService;
            langService.CurrentWorkspaceSettings.SqlTools.IntelliSense.EnableIntellisense = true;

            var completionItemsReleased = new TaskCompletionSource<CompletionItem[]>();
            blockingLanguageService.GetCompletionItemsCallback = async (textDocumentPosition, scriptFile, connInfo) =>
            {
                blockingLanguageService.GetCompletionItemsStarted.Set();
                var completionItems = await completionItemsReleased.Task;
                blockingLanguageService.GetCompletionItemsCompleted.Set();
                return completionItems;
            };

            var dispatcher = CreateDispatcher();
            dispatcher.ParallelMessageProcessing = true;
            dispatcher.SetRequestHandler(
                CompletionRequest.Type,
                blockingLanguageService.HandleCompletionRequest,
                overrideExisting: false,
                isParallelProcessingSupported: true);
            Assert.True(dispatcher.requestHandlerParallelismMap[CompletionRequest.Type.MethodName]);

            var dispatcherProbeHandled = new ManualResetEventSlim(initialState: false);
            dispatcher.SetRequestHandler(
                DispatcherProbeRequest,
                async (value, requestContext) =>
                {
                    dispatcherProbeHandled.Set();
                    await requestContext.SendResult(value);
                });

            var stalledDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "1",
                    method: CompletionRequest.Type.MethodName,
                    contents: JToken.FromObject(textDocument)));

            Assert.True(
                stalledDispatchTask.Wait(1000),
                "Expected completion handler dispatch to return without waiting for the inner operation");
            Assert.True(
                blockingLanguageService.GetCompletionItemsStarted.Wait(TaskTimeout),
                "Expected stalled completion work to start in the background");
            Assert.False(
                blockingLanguageService.GetCompletionItemsCompleted.IsSet,
                "Expected completion work to remain stalled before releasing it");

            var followUpDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "2",
                    method: DispatcherProbeRequest.MethodName,
                    contents: JToken.FromObject(1)));

            Assert.True(
                followUpDispatchTask.Wait(1000),
                "Expected the dispatcher to process a second request while completion work is stalled");
            Assert.True(
                dispatcherProbeHandled.Wait(1000),
                "Expected the follow-up dispatcher request to be handled while completion work is stalled");

            completionItemsReleased.SetResult(Array.Empty<CompletionItem>());
            Assert.True(
                blockingLanguageService.GetCompletionItemsCompleted.Wait(TaskTimeout),
                "Expected completion work to finish after being released");
        }

        [Test]
        public void HandleLanguageFlavorChangeAndRebuild_SameUri_AreSerializedWithoutBlockingDispatcher()
        {
            InitializeTestObjects();

            var blockingLanguageService = (BlockingLanguageService)langService;
            blockingLanguageService.DoHandleRebuildIntellisenseNotificationCallback = (rebuildParams, eventContext) =>
            {
                int invocationCount = Interlocked.Increment(ref blockingLanguageService.DoHandleRebuildInvocationCount);
                if (invocationCount == 1)
                {
                    blockingLanguageService.DoHandleRebuildStarted.Set();
                    Assert.True(
                        blockingLanguageService.AllowDoHandleRebuildToContinue.Wait(TaskTimeout),
                        "Expected the first rebuild operation to be released by the test");
                }
                else
                {
                    blockingLanguageService.SecondDoHandleRebuildStarted.Set();
                }

                return Task.CompletedTask;
            };

            var dispatcher = CreateDispatcher();
            dispatcher.ParallelMessageProcessing = true;
            dispatcher.SetEventHandler(
                RebuildIntelliSenseNotification.Type,
                blockingLanguageService.HandleRebuildIntelliSenseNotification,
                overrideExisting: false,
                isParallelProcessingSupported: true);
            dispatcher.SetEventHandler(
                LanguageFlavorChangeNotification.Type,
                blockingLanguageService.HandleDidChangeLanguageFlavorNotification,
                overrideExisting: false,
                isParallelProcessingSupported: true);
            Assert.True(dispatcher.eventHandlerParallelismMap[RebuildIntelliSenseNotification.Type.MethodName]);
            Assert.True(dispatcher.eventHandlerParallelismMap[LanguageFlavorChangeNotification.Type.MethodName]);

            var dispatcherProbeHandled = new ManualResetEventSlim(initialState: false);
            dispatcher.SetRequestHandler(
                DispatcherProbeRequest,
                async (value, requestContext) =>
                {
                    dispatcherProbeHandled.Set();
                    await requestContext.SendResult(value);
                });

            var languageFlavorDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Event(
                    LanguageFlavorChangeNotification.Type.MethodName,
                    JToken.FromObject(new LanguageFlavorChangeParams
                    {
                        Uri = testScriptUri,
                        Language = LanguageService.SQL_LANG,
                        Flavor = Microsoft.SqlTools.ServiceLayer.Hosting.ServiceHost.ProviderName,
                    })));

            Assert.True(
                languageFlavorDispatchTask.Wait(1000),
                "Expected language flavor change dispatch to return without waiting for same-URI rebuild work");
            Assert.True(
                blockingLanguageService.DoHandleRebuildStarted.Wait(TaskTimeout),
                "Expected language flavor change to trigger rebuild work");

            var rebuildDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Event(
                    RebuildIntelliSenseNotification.Type.MethodName,
                    JToken.FromObject(new RebuildIntelliSenseParams
                    {
                        OwnerUri = testScriptUri,
                    })));

            Assert.True(
                rebuildDispatchTask.Wait(1000),
                "Expected same-URI rebuild dispatch to return while waiting on the URI gate");
            Assert.False(
                blockingLanguageService.SecondDoHandleRebuildStarted.Wait(300),
                "Expected same-URI rebuild work to wait for the first gated operation to finish");

            var followUpDispatchTask = dispatcher.DispatchMessageForTest(
                Message.Request(
                    id: "3",
                    method: DispatcherProbeRequest.MethodName,
                    contents: JToken.FromObject(1)));

            Assert.True(
                followUpDispatchTask.Wait(1000),
                "Expected the dispatcher to process unrelated work while same-URI gated work is stalled");
            Assert.True(
                dispatcherProbeHandled.Wait(1000),
                "Expected unrelated dispatcher work to run while the first rebuild is stalled");

            blockingLanguageService.AllowDoHandleRebuildToContinue.Set();

            Assert.True(
                blockingLanguageService.SecondDoHandleRebuildStarted.Wait(TaskTimeout),
                "Expected same-URI rebuild work to start after the first gated operation completed");
            Assert.AreEqual(2, Volatile.Read(ref blockingLanguageService.DoHandleRebuildInvocationCount));
        }

        [Test]
        public void HandleDidChangeLanguageFlavorNotification_SameUri_CompletesInOrder()
        {
            InitializeTestObjects();

            var blockingLanguageService = (BlockingLanguageService)langService;
            var completionOrder = new ConcurrentQueue<int>();
            var firstCompletionObserved = new ManualResetEventSlim(initialState: false);
            var secondCompletionObserved = new ManualResetEventSlim(initialState: false);

            blockingLanguageService.DoHandleRebuildIntellisenseNotificationCallback = (rebuildParams, eventContext) =>
            {
                int invocationCount = Interlocked.Increment(ref blockingLanguageService.DoHandleRebuildInvocationCount);
                if (invocationCount == 1)
                {
                    blockingLanguageService.DoHandleRebuildStarted.Set();
                    Assert.True(
                        blockingLanguageService.AllowDoHandleRebuildToContinue.Wait(TaskTimeout),
                        "Expected the first language flavor change to be released by the test");
                    completionOrder.Enqueue(invocationCount);
                    firstCompletionObserved.Set();
                }
                else
                {
                    blockingLanguageService.SecondDoHandleRebuildStarted.Set();
                    completionOrder.Enqueue(invocationCount);
                    secondCompletionObserved.Set();
                }

                return Task.CompletedTask;
            };

            var dispatcher = CreateDispatcher();
            dispatcher.ParallelMessageProcessing = true;
            dispatcher.SetEventHandler(
                LanguageFlavorChangeNotification.Type,
                blockingLanguageService.HandleDidChangeLanguageFlavorNotification,
                overrideExisting: false,
                isParallelProcessingSupported: true);
            Assert.True(dispatcher.eventHandlerParallelismMap[LanguageFlavorChangeNotification.Type.MethodName]);

            Message firstLanguageFlavorEvent = Message.Event(
                LanguageFlavorChangeNotification.Type.MethodName,
                JToken.FromObject(new LanguageFlavorChangeParams
                {
                    Uri = testScriptUri,
                    Language = LanguageService.SQL_LANG,
                    Flavor = Microsoft.SqlTools.ServiceLayer.Hosting.ServiceHost.ProviderName,
                }));

            var firstDispatchTask = dispatcher.DispatchMessageForTest(firstLanguageFlavorEvent);

            Assert.True(
                firstDispatchTask.Wait(1000),
                "Expected the first language flavor change dispatch to return without waiting for rebuild work");
            Assert.True(
                blockingLanguageService.DoHandleRebuildStarted.Wait(TaskTimeout),
                "Expected the first language flavor change to start rebuild work");

            var secondDispatchTask = dispatcher.DispatchMessageForTest(firstLanguageFlavorEvent);

            Assert.True(
                secondDispatchTask.Wait(1000),
                "Expected the second language flavor change dispatch to return while waiting on the URI gate");
            Assert.False(
                blockingLanguageService.SecondDoHandleRebuildStarted.Wait(300),
                "Expected the second language flavor change to remain queued behind the first one");

            blockingLanguageService.AllowDoHandleRebuildToContinue.Set();

            Assert.True(
                firstCompletionObserved.Wait(TaskTimeout),
                "Expected the first language flavor change to complete after being released");
            Assert.True(
                secondCompletionObserved.Wait(TaskTimeout),
                "Expected the second language flavor change to complete after the first one finished");
            CollectionAssert.AreEqual(new[] { 1, 2 }, completionOrder.ToArray());
        }

        private static TestMessageDispatcher CreateDispatcher()
        {
            return new TestMessageDispatcher(new TestChannel());
        }

        private static TestMessageDispatcher CreateQueuedDispatcher(out QueuedTestChannel queuedChannel)
        {
            queuedChannel = new QueuedTestChannel();
            return new TestMessageDispatcher(queuedChannel);
        }

        private sealed class BlockingLanguageService : LanguageService
        {
            internal readonly ManualResetEventSlim ResolveCompletionStarted = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim AllowResolveCompletionToContinue = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim ResolveCompletionCompleted = new ManualResetEventSlim(initialState: false);

            internal readonly ManualResetEventSlim GetCompletionItemsStarted = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim GetCompletionItemsCompleted = new ManualResetEventSlim(initialState: false);

            internal readonly ManualResetEventSlim GetHoverItemStarted = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim AllowGetHoverItemToContinue = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim GetHoverItemCompleted = new ManualResetEventSlim(initialState: false);

            internal readonly ManualResetEventSlim GetSignatureHelpStarted = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim AllowGetSignatureHelpToContinue = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim GetSignatureHelpCompleted = new ManualResetEventSlim(initialState: false);

            internal readonly ManualResetEventSlim GetDefinitionStarted = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim AllowGetDefinitionToContinue = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim GetDefinitionCompleted = new ManualResetEventSlim(initialState: false);

            internal readonly ManualResetEventSlim DoHandleRebuildStarted = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim AllowDoHandleRebuildToContinue = new ManualResetEventSlim(initialState: false);
            internal readonly ManualResetEventSlim SecondDoHandleRebuildStarted = new ManualResetEventSlim(initialState: false);

            internal int DoHandleRebuildInvocationCount;

            internal Func<CompletionItem, CompletionItem> ResolveCompletionItemCallback { get; set; }

            internal Func<TextDocumentPosition, ScriptFile, ConnectionInfo, Task<CompletionItem[]>> GetCompletionItemsCallback { get; set; }

            internal Func<TextDocumentPosition, ScriptFile, Hover> GetHoverItemCallback { get; set; }

            internal Func<TextDocumentPosition, ScriptFile, Task<SignatureHelp>> GetSignatureHelpCallback { get; set; }

            internal Func<TextDocumentPosition, ScriptFile, ConnectionInfo, DefinitionResult> GetDefinitionCallback { get; set; }

            internal Func<RebuildIntelliSenseParams, EventContext, Task> DoHandleRebuildIntellisenseNotificationCallback { get; set; }

            public override Task<CompletionItem[]> GetCompletionItems(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile, ConnectionInfo connInfo, CancellationToken cancellationToken = default)
            {
                return GetCompletionItemsCallback != null
                    ? GetCompletionItemsCallback(textDocumentPosition, scriptFile, connInfo)
                    : base.GetCompletionItems(textDocumentPosition, scriptFile, connInfo, cancellationToken);
            }

            internal override CompletionItem ResolveCompletionItem(CompletionItem completionItem)
            {
                return ResolveCompletionItemCallback != null
                    ? ResolveCompletionItemCallback(completionItem)
                    : base.ResolveCompletionItem(completionItem);
            }

            internal override Hover GetHoverItem(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile)
            {
                return GetHoverItemCallback != null
                    ? GetHoverItemCallback(textDocumentPosition, scriptFile)
                    : base.GetHoverItem(textDocumentPosition, scriptFile);
            }

            internal override Task<SignatureHelp> GetSignatureHelp(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile)
            {
                return GetSignatureHelpCallback != null
                    ? GetSignatureHelpCallback(textDocumentPosition, scriptFile)
                    : base.GetSignatureHelp(textDocumentPosition, scriptFile);
            }

            internal override DefinitionResult GetDefinition(TextDocumentPosition textDocumentPosition, ScriptFile scriptFile, ConnectionInfo connInfo)
            {
                return GetDefinitionCallback != null
                    ? GetDefinitionCallback(textDocumentPosition, scriptFile, connInfo)
                    : base.GetDefinition(textDocumentPosition, scriptFile, connInfo);
            }

            public override Task DoHandleRebuildIntellisenseNotification(RebuildIntelliSenseParams rebuildParams, EventContext eventContext)
            {
                return DoHandleRebuildIntellisenseNotificationCallback != null
                    ? DoHandleRebuildIntellisenseNotificationCallback(rebuildParams, eventContext)
                    : base.DoHandleRebuildIntellisenseNotification(rebuildParams, eventContext);
            }
        }

        private sealed class TestMessageDispatcher : MessageDispatcher
        {
            internal TestMessageDispatcher(ChannelBase protocolChannel)
                : base(protocolChannel)
            {
            }

            internal Task DispatchMessageForTest(Message message)
            {
                return this.DispatchMessage(message, this.MessageWriter);
            }
        }

        private sealed class TestChannel : ChannelBase
        {
            internal TestChannel()
            {
                this.MessageWriter = new MessageWriter(new MemoryStream(), new JsonRpcMessageSerializer());
            }

            public override Task WaitForConnection()
            {
                return Task.CompletedTask;
            }

            protected override void Initialize(IMessageSerializer messageSerializer, Stream inputStream = null, Stream outputStream = null)
            {
            }

            protected override void Shutdown()
            {
            }
        }

        private sealed class QueuedTestChannel : ChannelBase
        {
            private readonly QueuedTestMessageReader messageReader = new QueuedTestMessageReader();

            internal QueuedTestChannel()
            {
                this.MessageReader = this.messageReader;
                this.MessageWriter = new MessageWriter(new MemoryStream(), new JsonRpcMessageSerializer());
            }

            internal void QueueMessage(Message message)
            {
                this.messageReader.QueueMessage(message);
            }

            internal void Complete()
            {
                this.messageReader.Complete();
            }

            public override Task WaitForConnection()
            {
                return Task.CompletedTask;
            }

            protected override void Initialize(IMessageSerializer messageSerializer, Stream inputStream = null, Stream outputStream = null)
            {
            }

            protected override void Shutdown()
            {
            }
        }

        private sealed class QueuedTestMessageReader : MessageReader
        {
            private readonly SemaphoreSlim availableMessages = new SemaphoreSlim(0);
            private readonly ConcurrentQueue<Message> messages = new ConcurrentQueue<Message>();

            internal void QueueMessage(Message message)
            {
                messages.Enqueue(message);
                availableMessages.Release();
            }

            internal void Complete()
            {
                QueueMessage(null);
            }

            public override async Task<Message> ReadMessage()
            {
                await availableMessages.WaitAsync();

                messages.TryDequeue(out Message message);
                if (message == null)
                {
                    throw new EndOfStreamException();
                }

                return message;
            }
        }
    }
}