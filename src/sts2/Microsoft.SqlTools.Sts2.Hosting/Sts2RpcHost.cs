//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.SqlTools.Sts2.Hosting
{
    /// <summary>
    /// The STS2 StreamJsonRpc gateway (SPEC §3.1). Attaches to the multiplexer's virtual
    /// STS2 stream pair and serves <c>v2/</c> methods. At M0 only <c>v2/diagnostics.ping</c>
    /// exists; envelope translation and the coordinator arrive in M1.
    /// </summary>
    public sealed class Sts2RpcHost : IAsyncDisposable
    {
        private readonly JsonRpc rpc;

        private Sts2RpcHost(JsonRpc rpc)
        {
            this.rpc = rpc;
        }

        /// <summary>Completes when the RPC connection closes; faults if the host crashes.</summary>
        public Task Completion => rpc.Completion;

        /// <summary>
        /// Attaches an STS2 host to the given streams (from the service's perspective:
        /// <paramref name="input"/> is read, <paramref name="output"/> is written) and
        /// starts listening.
        /// </summary>
        public static Sts2RpcHost Attach(Stream input, Stream output, string serviceVersion)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(output);
            ArgumentException.ThrowIfNullOrEmpty(serviceVersion);

            var formatter = new SystemTextJsonFormatter();
            formatter.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var handler = new HeaderDelimitedMessageHandler(output, input, formatter);
            var rpc = new JsonRpc(handler);
            rpc.AddLocalRpcTarget(new DiagnosticsRpcTarget(serviceVersion), null);
            rpc.StartListening();
            return new Sts2RpcHost(rpc);
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            rpc.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
