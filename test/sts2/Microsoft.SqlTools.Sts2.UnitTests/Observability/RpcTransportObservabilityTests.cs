//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Sts2.Hosting;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Observability
{
    public sealed class RpcTransportObservabilityTests
    {
        [Fact]
        public async Task MeasuresSerializationCopyAndFlushWithoutCapturingPayload()
        {
            var snapshots = new List<string>();
            var stats = new RpcTransportStats(snapshots.Add);
            var inner = new SystemTextJsonFormatter();
            inner.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var formatter = new MeasuredSystemTextJsonFormatter(inner, stats);
            using var output = new MemoryStream();
            using var input = new MemoryStream();
            var handler = new MeasuredHeaderDelimitedMessageHandler(output, input, formatter, stats);
            string privateValue = "private-canary-" + new string('x', 100_000);

            await handler.WriteAsync(
                CreateNotification(formatter, "v2/query.rows", new { value = privateValue }),
                CancellationToken.None);
            Assert.Empty(snapshots);

            await handler.WriteAsync(
                CreateNotification(formatter, "v2/query.complete", new { status = "succeeded" }),
                CancellationToken.None);

            string snapshot = Assert.Single(snapshots);
            Assert.DoesNotContain("private-canary", snapshot);
            using JsonDocument document = JsonDocument.Parse(snapshot);
            JsonElement root = document.RootElement;
            Assert.Equal("sts2.rpc.transport.stats/1", root.GetProperty("schema").GetString());
            Assert.Equal(0, root.GetProperty("directPipeEndpoint").GetInt32());
            Assert.Equal(2, root.GetProperty("messages").GetInt64());
            Assert.Equal(1, root.GetProperty("rowMessages").GetInt64());
            Assert.True(root.GetProperty("rowBytes").GetInt64() > privateValue.Length);
            Assert.Equal(2, root.GetProperty("writeCalls").GetInt64());
            Assert.Equal(2, root.GetProperty("flushCalls").GetInt64());
            Assert.Equal(0, root.GetProperty("serializationFailures").GetInt64());
            Assert.Equal(0, root.GetProperty("writeFailures").GetInt64());
            Assert.Equal(0, root.GetProperty("flushFailures").GetInt64());

            string wire = System.Text.Encoding.UTF8.GetString(output.ToArray());
            Assert.Contains("Content-Length:", wire);
            Assert.Contains("\"method\":\"v2/query.rows\"", wire);
            Assert.Contains(privateValue, wire);
        }

        [Fact]
        public async Task WritesExactFramingThroughDirectPipeEndpoint()
        {
            var snapshots = new List<string>();
            var stats = new RpcTransportStats(snapshots.Add, directPipeEndpoint: true);
            var inner = new SystemTextJsonFormatter();
            inner.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var formatter = new MeasuredSystemTextJsonFormatter(inner, stats);
            var output = new Pipe();
            var input = new Pipe();
            using var handler = new MeasuredHeaderDelimitedMessageHandler(
                output.Writer,
                input.Reader,
                formatter,
                stats);

            await handler.WriteAsync(
                CreateNotification(formatter, "v2/query.complete", new { status = "succeeded" }),
                CancellationToken.None);

            ReadResult result = await output.Reader.ReadAsync(CancellationToken.None);
            string wire = System.Text.Encoding.UTF8.GetString(result.Buffer.ToArray());
            output.Reader.AdvanceTo(result.Buffer.End);
            Assert.Contains("Content-Length:", wire);
            Assert.Contains("\"method\":\"v2/query.complete\"", wire);
            using JsonDocument snapshot = JsonDocument.Parse(Assert.Single(snapshots));
            Assert.Equal(1, snapshot.RootElement.GetProperty("directPipeEndpoint").GetInt32());
        }

        private static JsonRpcRequest CreateNotification(
            MeasuredSystemTextJsonFormatter formatter,
            string method,
            object parameters)
        {
            JsonRpcRequest request = ((IJsonRpcMessageFactory)formatter).CreateRequestMessage();
            request.Method = method;
            request.Arguments = parameters;
            return request;
        }
    }
}
