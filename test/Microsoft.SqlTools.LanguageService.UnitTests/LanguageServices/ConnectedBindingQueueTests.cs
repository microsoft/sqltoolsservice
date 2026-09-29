//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Common;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.LanguageService.UnitTests.LanguageServices
{
    /// <summary>
    /// Races between creating, replacing and using connection and project binding contexts
    /// </summary>
    public class ConnectedBindingQueueTests
    {
        private ConnectedBindingQueue connectedQueue;

        private TestConnectionOpener opener;

        [SetUp]
        public void CreateQueue()
        {
            this.opener = new TestConnectionOpener();
            this.connectedQueue = new ConnectedBindingQueue(needsMetadata: false);
            this.connectedQueue.SetConnectionOpener(this.opener);
        }

        [TearDown]
        public void DisposeQueue()
        {
            this.opener.ReleaseOpens();
            this.connectedQueue.Dispose();
        }

        /// <summary>
        /// Editors, Object Explorer and the file browser add the same connection's context at once.
        /// Exactly one of them may create and connect it, and every caller must see it connected
        /// once its call returns, or an editor is left without IntelliSense.
        /// </summary>
        [Test]
        [Timeout(30_000)]
        public async Task ConcurrentAddsCreateAndConnectOneContext()
        {
            this.opener.OpenDelay = TimeSpan.FromMilliseconds(50);
            TestConnectionInfo connectionInfo = CreateConnectionInfo();
            var start = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            int sawDisconnected = 0;

            Task<string>[] adds = Enumerable.Range(0, 32).Select(_ => Task.Run(async () =>
            {
                await start.Task;
                string key = await this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
                if (!this.connectedQueue.IsBindingContextConnected(key))
                {
                    Interlocked.Increment(ref sawDisconnected);
                }
                return key;
            })).ToArray();
            start.SetResult(null);
            string[] keys = await Task.WhenAll(adds);

            Assert.That(sawDisconnected, Is.Zero, "Every caller sees the context connected when its call returns.");
            Assert.That(keys.Distinct(), Is.EqualTo(new[] { connectionInfo.ConnectionContextKey }));
            Assert.That(this.opener.OpenCount, Is.EqualTo(1), "Only one caller opens the connection.");
            Assert.That(this.connectedQueue.BindingContextMap.Count, Is.EqualTo(1));
            Assert.That(this.connectedQueue.IsBindingContextConnected(connectionInfo.ConnectionContextKey), Is.True);
        }

        /// <summary>
        /// A caller that finds a context another caller is still connecting must wait for it,
        /// not return while the context still reports it is not connected.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task ReusingCallerWaitsForTheContextToConnect()
        {
            this.opener.HoldOpens();
            TestConnectionInfo connectionInfo = CreateConnectionInfo();

            Task<string> first = this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
            await this.opener.OpenStarted;
            Task<string> second = this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
            await Task.Delay(100);
            Assert.That(second.IsCompleted, Is.False, "The second caller waits for the first to connect.");

            this.opener.ReleaseOpens();
            await first;
            string key = await second;

            Assert.That(this.connectedQueue.IsBindingContextConnected(key), Is.True);
            Assert.That(this.opener.OpenCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Removing a context while another caller waits for it to connect must not cause
        /// that caller to recreate the connection after the session has closed.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task ReusingCallerDoesNotRecreateAContextRemovedWhileConnecting()
        {
            this.opener.HoldOpens();
            TestConnectionInfo connectionInfo = CreateConnectionInfo();

            Task<string> first = this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
            await this.opener.OpenStarted;
            Task<string> second = this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
            Assert.That(second.IsCompleted, Is.False);

            this.connectedQueue.RemoveConnectionContext(connectionInfo);
            this.opener.ReleaseOpens();
            await first;

            Assert.ThrowsAsync<OperationCanceledException>(async () => { await second; });
            Assert.That(second.IsCanceled, Is.True);
            Assert.That(this.connectedQueue.BindingContextMap.ContainsKey(connectionInfo.ConnectionContextKey), Is.False);
            Assert.That(this.opener.OpenCount, Is.EqualTo(1), "Removal must not trigger another connection open.");
        }

        /// <summary>
        /// A context is visible as soon as it is added, before its connection is open. An
        /// operation queued in that window must wait and then see the populated context.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task OperationQueuedWhileTheContextConnectsSeesItConnected()
        {
            this.opener.HoldOpens();
            TestConnectionInfo connectionInfo = CreateConnectionInfo();

            Task<string> add = this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
            await this.opener.OpenStarted;
            Task<QueueItem> operation = this.connectedQueue.QueueBindingOperationAsync(
                connectionInfo.ConnectionContextKey,
                waitForLockTimeout: 5_000,
                bindOperation: (context, cancellationToken) => context.IsConnected ? context.ServerConnection : null);
            await Task.Delay(100);
            Assert.That(operation.IsCompleted, Is.False, "The operation waits for the connection to open.");

            this.opener.ReleaseOpens();
            await add;
            QueueItem item = await operation;

            Assert.That(item.WasExecuted, Is.True);
            Assert.That(item.Result, Is.Not.Null, "The operation must not run on a half-built context.");
        }

        /// <summary>
        /// Rebuilding IntelliSense overwrites a connection's context. Overwrites that race each
        /// other, including one that lands while another is still connecting, must leave a
        /// connected context and let every caller finish.
        /// </summary>
        [Test]
        [Timeout(30_000)]
        public async Task ConcurrentOverwritesLeaveAConnectedContext()
        {
            this.opener.OpenDelay = TimeSpan.FromMilliseconds(20);
            TestConnectionInfo connectionInfo = CreateConnectionInfo();

            await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
                this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test", overwrite: true))));

            Assert.That(this.connectedQueue.BindingContextMap.Count, Is.EqualTo(1));
            Assert.That(this.connectedQueue.BindingContextMap.TryGetValue(connectionInfo.ConnectionContextKey, out IBindingContext context), Is.True);
            Assert.That(context.IsConnected, Is.True);
            Assert.That(context.ServerConnection, Is.Not.Null);
            Assert.That(await context.BindingLock.WaitAsync(0), Is.True, "No overwrite leaves the published context locked.");
            context.BindingLock.Release();
        }

        /// <summary>
        /// Every project save replaces the project's context while completions and diagnostics are
        /// queued for it. No operation may ever see a context without its binder.
        /// </summary>
        [Test]
        [Timeout(30_000)]
        public async Task ProjectContextReplacementsNeverExposeAHalfBuiltContext()
        {
            const string projectKey = "project_test";
            IBinder binder = new Mock<IBinder>().Object;
            var parseOptions = new ParseOptions(
                batchSeparator: "GO",
                isQuotedIdentifierSet: true,
                compatibilityLevel: DatabaseCompatibilityLevel.Current,
                transactSqlVersion: TransactSqlVersion.Current);
            this.connectedQueue.AddProjectContext(projectKey, binder, parseOptions);
            int halfBuilt = 0;

            Task replacements = Task.Run(() =>
            {
                for (int i = 0; i < 500; i++)
                {
                    this.connectedQueue.AddProjectContext(projectKey, binder, parseOptions);
                }
            });
            QueueItem[] results = await Task.WhenAll(Enumerable.Range(0, 500).Select(_ => Task.Run(() =>
                this.connectedQueue.QueueBindingOperationAsync(
                    projectKey,
                    waitForLockTimeout: 10_000,
                    bindOperation: (context, cancellationToken) =>
                    {
                        if (!(context is ConnectedBindingContext { IsProjectContext: true } project)
                            || project.Binder == null
                            || project.ProjectParseOptions == null)
                        {
                            Interlocked.Increment(ref halfBuilt);
                        }
                        return null;
                    }))));
            await replacements;

            Assert.That(results.All(r => r.WasExecuted), Is.True);
            Assert.That(halfBuilt, Is.Zero);
        }

        /// <summary>
        /// A restore closes every connection to its database. It must wait for a context that is
        /// still connecting rather than skip it, or the restore starts while that context connects.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task CloseConnectionsWaitsForAContextThatIsStillConnecting()
        {
            this.opener.HoldOpens();
            TestConnectionInfo connectionInfo = CreateConnectionInfo();
            Task<string> add = this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
            await this.opener.OpenStarted;

            Task close = this.connectedQueue.CloseConnectionsAsync("server", "db", 10_000);
            await Task.Delay(100);
            Assert.That(close.IsCompleted, Is.False, "A restore must not proceed while a context is still connecting.");

            this.opener.ReleaseOpens();
            await add;
            await close;
        }

        [Test]
        [Timeout(10_000)]
        public async Task AddingAContextAfterDisposeIsRejected()
        {
            this.connectedQueue.Dispose();

            ObjectDisposedException thrown = null;
            try
            {
                await this.connectedQueue.AddConnectionContextAsync(CreateConnectionInfo(), "test");
            }
            catch (ObjectDisposedException ex)
            {
                thrown = ex;
            }

            Assert.That(thrown, Is.Not.Null);
            Assert.That(this.opener.OpenCount, Is.Zero, "No connection is opened.");
            Assert.That(this.connectedQueue.BindingContextMap, Is.Empty);
        }

        /// <summary>
        /// Disposal can land while a context is still connecting, before it has a connection for
        /// Dispose to close. The add must close the connection it opened and report the disposal.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task DisposingWhileAContextConnectsClosesItsConnection()
        {
            this.opener.HoldOpens();
            TestConnectionInfo connectionInfo = CreateConnectionInfo();
            Task<string> add = this.connectedQueue.AddConnectionContextAsync(connectionInfo, "test");
            await this.opener.OpenStarted;

            this.connectedQueue.Dispose();
            this.opener.ReleaseOpens();

            ObjectDisposedException thrown = null;
            try
            {
                await add;
            }
            catch (ObjectDisposedException ex)
            {
                thrown = ex;
            }

            Assert.That(thrown, Is.Not.Null);
            Assert.That(this.connectedQueue.IsBindingContextConnected(connectionInfo.ConnectionContextKey), Is.False,
                "The connection opened during disposal is closed.");
        }

        private static TestConnectionInfo CreateConnectionInfo()
        {
            return new TestConnectionInfo(new ConnectionDetails
            {
                ServerName = "server",
                DatabaseName = "db",
                UserName = "user",
                AuthenticationType = "SqlLogin"
            });
        }

        private sealed class TestConnectionInfo : ConnectionInfoBase
        {
            public TestConnectionInfo(ConnectionDetails connectionDetails)
                : base("test-owner-uri", connectionDetails)
            {
            }

            public override bool IsCloud { get; set; }

            public override bool TryGetConnection(string connectionType, out DbConnection connection)
            {
                connection = null;
                return false;
            }
        }

        /// <summary>
        /// Returns unopened server connections, counting them, and can hold opens in progress
        /// </summary>
        private sealed class TestConnectionOpener : SqlConnectionOpener
        {
            private readonly ManualResetEventSlim opensReleased = new ManualResetEventSlim(initialState: true);

            private readonly TaskCompletionSource<object> openStarted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            private int openCount;

            public TimeSpan OpenDelay { get; set; }

            public int OpenCount => Volatile.Read(ref this.openCount);

            public Task OpenStarted => this.openStarted.Task;

            public void HoldOpens() => this.opensReleased.Reset();

            public void ReleaseOpens() => this.opensReleased.Set();

            public override ServerConnection OpenServerConnection(ConnectionInfoBase connInfo, string featureName)
            {
                Interlocked.Increment(ref this.openCount);
                this.openStarted.TrySetResult(null);
                this.opensReleased.Wait();
                if (this.OpenDelay > TimeSpan.Zero)
                {
                    Thread.Sleep(this.OpenDelay);
                }
                return new ServerConnection();
            }
        }
    }
}
