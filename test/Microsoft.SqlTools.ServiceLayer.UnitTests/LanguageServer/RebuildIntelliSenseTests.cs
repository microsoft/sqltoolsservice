//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    public class RebuildIntelliSenseTests : LanguageServiceTestBase<object>
    {
        private sealed class SerializedRebuildLanguageService : TSqlLanguageService
        {
            private readonly TaskCompletionSource<bool> releaseFirstRebuild =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            internal TaskCompletionSource<bool> FirstRebuildStarted { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            internal int RebuildCount => Volatile.Read(ref this.rebuildCount);

            internal bool ConcurrentRebuildObserved => Volatile.Read(ref this.concurrentRebuildObserved) != 0;

            private int activeRebuildCount;
            private int concurrentRebuildObserved;
            private int rebuildCount;

            internal void ReleaseFirstRebuild()
            {
                this.releaseFirstRebuild.TrySetResult(true);
            }

            public override async Task DoHandleRebuildIntellisenseNotification(
                RebuildIntelliSenseParams rebuildParams,
                EventContext eventContext)
            {
                int rebuildNumber = Interlocked.Increment(ref this.rebuildCount);
                if (Interlocked.Increment(ref this.activeRebuildCount) > 1)
                {
                    Interlocked.Exchange(ref this.concurrentRebuildObserved, 1);
                }

                try
                {
                    if (rebuildNumber == 1)
                    {
                        this.FirstRebuildStarted.TrySetResult(true);
                        await this.releaseFirstRebuild.Task;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref this.activeRebuildCount);
                }
            }
        }

        /// <summary>
        /// Verifies that refresh releases the old thread-affine metadata <see cref="Monitor"/>
        /// before starting the asynchronous metadata update. Holding the monitor across the
        /// update allows an awaited continuation on another thread to throw a
        /// <see cref="SynchronizationLockException"/> when it tries to release the monitor.
        /// A separate thread probes the old lock when the update starts so the test does not
        /// depend on task scheduling or database performance.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task RebuildReleasesMetadataMonitorBeforeStartingMetadataUpdate()
        {
            InitializeTestObjects();
            workspaceService.Object.CurrentSettings.SqlTools.IntelliSense.EnableErrorChecking = false;

            var serviceHost = new Mock<ILanguageServiceHost>();
            serviceHost
                .Setup(host => host.SendEvent(
                    It.IsAny<EventType<IntelliSenseReadyParams>>(),
                    It.IsAny<IntelliSenseReadyParams>()))
                .Returns(Task.CompletedTask);
            langService.ServiceHostInstance = serviceHost.Object;

            object oldMetadataLock = scriptParseInfo.BuildingMetadataLock;
            bool oldMetadataLockWasAvailable = false;
            bool metadataUpdateStarted = false;

            bindingQueue
                .Setup(queue => queue.AddConnectionContext(
                    It.IsAny<ConnectionInfoBase>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .Callback((ConnectionInfoBase connectionInfo, string featureName, bool overwrite) =>
                {
                    if (!overwrite)
                    {
                        metadataUpdateStarted = true;
                        oldMetadataLockWasAvailable = CanAcquireFromAnotherThread(oldMetadataLock);
                    }
                })
                .Returns(this.testConnectionKey);

            try
            {
                await langService.DoHandleRebuildIntellisenseNotification(
                    new RebuildIntelliSenseParams { OwnerUri = this.testScriptUri },
                    eventContext: null);

                Assert.That(metadataUpdateStarted, Is.True, "The asynchronous metadata update should start.");
                Assert.That(
                    oldMetadataLockWasAvailable,
                    Is.True,
                    "The old metadata monitor must be released before the metadata update starts.");
                Assert.That(
                    langService.GetScriptParseInfo(this.testScriptUri),
                    Is.Not.SameAs(scriptParseInfo),
                    "Refresh should replace the old script parse information.");
                serviceHost.Verify(
                    host => host.SendEvent(
                        It.IsAny<EventType<IntelliSenseReadyParams>>(),
                        It.Is<IntelliSenseReadyParams>(parameters => parameters.OwnerUri == this.testScriptUri)),
                    Times.AtLeastOnce);
            }
            finally
            {
                langService.Dispose();
            }
        }

        /// <summary>
        /// Verifies that moving the metadata rebuild outside the synchronous monitor does not
        /// allow two refreshes for the same editor to run concurrently. The first refresh is
        /// paused while the second is submitted, proving that the URI-specific asynchronous lock
        /// continues to serialize the complete refresh operation.
        /// </summary>
        [Test]
        [Timeout(10_000)]
        public async Task RebuildRequestsForSameUriRemainSerialized()
        {
            var service = new SerializedRebuildLanguageService();
            var rebuildParams = new RebuildIntelliSenseParams { OwnerUri = this.testScriptUri };

            try
            {
                Task firstRebuild = service.HandleRebuildIntelliSenseNotification(rebuildParams, eventContext: null);
                await service.FirstRebuildStarted.Task;

                Task secondRebuild = service.HandleRebuildIntelliSenseNotification(rebuildParams, eventContext: null);

                Assert.That(secondRebuild.IsCompleted, Is.False);
                Assert.That(service.RebuildCount, Is.EqualTo(1));

                service.ReleaseFirstRebuild();
                await Task.WhenAll(firstRebuild, secondRebuild);

                Assert.That(service.RebuildCount, Is.EqualTo(2));
                Assert.That(service.ConcurrentRebuildObserved, Is.False);
            }
            finally
            {
                service.ReleaseFirstRebuild();
                service.Dispose();
            }
        }

        private static bool CanAcquireFromAnotherThread(object lockObject)
        {
            bool lockWasAcquired = false;
            var probeThread = new Thread(() =>
            {
                if (Monitor.TryEnter(lockObject))
                {
                    try
                    {
                        lockWasAcquired = true;
                    }
                    finally
                    {
                        Monitor.Exit(lockObject);
                    }
                }
            })
            {
                IsBackground = true
            };

            probeThread.Start();
            return probeThread.Join(TimeSpan.FromSeconds(5)) && lockWasAcquired;
        }
    }
}
