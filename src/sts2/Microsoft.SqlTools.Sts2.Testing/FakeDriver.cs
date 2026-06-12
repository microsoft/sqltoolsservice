//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Abstractions;
using Microsoft.SqlTools.Sts2.Contracts;

namespace Microsoft.SqlTools.Sts2.Testing
{
    /// <summary>How one FakeDriver open attempt behaves (SPEC §10.4).</summary>
    public sealed record FakeOpenBehavior
    {
        /// <summary><c>ok</c>, <c>authFail</c>, <c>networkFail</c>, <c>timeout</c>, or <c>hang</c> (released only by cancellation).</summary>
        public string Outcome { get; init; } = "ok";

        /// <summary>Delay before the outcome materializes.</summary>
        public int DelayMs { get; init; }
    }

    /// <summary>
    /// Deterministic scripted driver (SPEC §10.4). M2 scope: open outcomes, hang points
    /// released by cancel, and session lease tracking. Query scripting lands in M3.
    /// </summary>
    public sealed class FakeDriver : IDbDriver
    {
        private readonly ConcurrentQueue<FakeOpenBehavior> openBehaviors = new();
        private int openSessions;

        /// <inheritdoc/>
        public string Name => "fake";

        /// <inheritdoc/>
        public DriverCapabilities Capabilities { get; } = new() { Dialects = ["neutral", "tsql"], Production = false };

        /// <summary>Live session count; I8 asserts this returns to zero by run end.</summary>
        public int OpenSessionCount => Volatile.Read(ref openSessions);

        /// <summary>Server facts every successful open reports.</summary>
        public ServerInfo ServerInfo { get; init; } = new()
        {
            Product = "Fake 1.0",
            Version = "1.0.0",
            EngineEdition = "Test",
            Dialect = "neutral",
        };

        /// <summary>Queues the behavior for the next open attempt; defaults to immediate <c>ok</c> when empty.</summary>
        public FakeDriver EnqueueOpen(FakeOpenBehavior behavior)
        {
            openBehaviors.Enqueue(behavior);
            return this;
        }

        /// <inheritdoc/>
        public async ValueTask<IDbSession> OpenAsync(ConnectionOpenRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            FakeOpenBehavior behavior = openBehaviors.TryDequeue(out FakeOpenBehavior? scripted) ? scripted : new FakeOpenBehavior();

            if (behavior.DelayMs > 0)
            {
                await Task.Delay(behavior.DelayMs, cancellationToken).ConfigureAwait(false);
            }

            switch (behavior.Outcome)
            {
                case "ok":
                    Interlocked.Increment(ref openSessions);
                    return new FakeSession(this);

                case "authFail":
                    throw new DbDriverException(Sts2ErrorCodes.ConnectionFailedAuth, "Login failed.",
                        new ServerErrorDetail { Number = 18456, Severity = 14, State = 1 });

                case "networkFail":
                    throw new DbDriverException(Sts2ErrorCodes.ConnectionFailedNetwork, "Network path not found.");

                case "timeout":
                    throw new DbDriverException(Sts2ErrorCodes.ConnectionFailedTimeout, "Connection attempt timed out.");

                case "hang":
                    // Released only by cancellation: the open-cancel scenarios depend on this.
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                    throw new InvalidOperationException("unreachable");

                default:
                    throw new DbDriverException(Sts2ErrorCodes.Internal, "Unknown scripted outcome: " + behavior.Outcome);
            }
        }

        private void SessionClosed() => Interlocked.Decrement(ref openSessions);

        private sealed class FakeSession : IDbSession
        {
            private readonly FakeDriver owner;
            private int disposed;

            internal FakeSession(FakeDriver owner)
            {
                this.owner = owner;
            }

            public ServerInfo Server => owner.ServerInfo;

            public async IAsyncEnumerable<ExecEvent> ExecuteAsync(QueryExecuteRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                // Query scripting arrives in M3.
                await Task.CompletedTask.ConfigureAwait(false);
                throw new DbDriverException(Sts2ErrorCodes.Internal, "FakeDriver query scripting lands in M3.");
#pragma warning disable CS0162 // unreachable: satisfies the async-iterator yield requirement
                yield break;
#pragma warning restore CS0162
            }

            public ValueTask CancelAsync(string queryId, CancellationToken cancellationToken) => ValueTask.CompletedTask;

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref disposed, 1) == 0)
                {
                    owner.SessionClosed();
                }
                return ValueTask.CompletedTask;
            }
        }
    }
}
