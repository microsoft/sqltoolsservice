//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Channel;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;
using Newtonsoft.Json.Linq;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    /// <summary>
    /// A real <see cref="ProtocolEndpoint"/> with a running message loop, connected over in-process pipes to a
    /// test client. Use it for tests that depend on how the message loop reads and dispatches messages.
    /// </summary>
    internal sealed class InMemoryProtocolConnection : IDisposable
    {
        /// <summary>
        /// How long the client waits for a message from the endpoint before failing the test.
        /// </summary>
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        private readonly AnonymousPipeServerStream clientToEndpoint = new AnonymousPipeServerStream(PipeDirection.Out);
        private readonly AnonymousPipeClientStream endpointInput;
        private readonly AnonymousPipeServerStream endpointToClient = new AnonymousPipeServerStream(PipeDirection.Out);
        private readonly AnonymousPipeClientStream clientInput;
        private readonly MessageReader clientReader;
        private readonly MessageWriter clientWriter;

        public InMemoryProtocolConnection(bool parallelMessageProcessing)
        {
            endpointInput = new AnonymousPipeClientStream(PipeDirection.In, clientToEndpoint.ClientSafePipeHandle);
            clientInput = new AnonymousPipeClientStream(PipeDirection.In, endpointToClient.ClientSafePipeHandle);
            clientReader = new MessageReader(clientInput, new JsonRpcMessageSerializer());
            clientWriter = new MessageWriter(clientToEndpoint, new JsonRpcMessageSerializer());

            Endpoint = new ProtocolEndpoint(new StreamChannel(), MessageProtocolType.LanguageServer);
            Endpoint.Initialize(endpointInput, endpointToClient);
            Endpoint.MessageDispatcher.ParallelMessageProcessing = parallelMessageProcessing;
            Endpoint.MessageDispatcher.Start();
        }

        public ProtocolEndpoint Endpoint { get; }

        /// <summary>
        /// Reads the next message the endpoint sent to the client.
        /// </summary>
        public Task<Message> ReadMessageAsync() => clientReader.ReadMessage().WaitAsync(Timeout);

        /// <summary>
        /// Sends a request from the client to the endpoint.
        /// </summary>
        public Task SendRequestAsync<TParams, TResult>(RequestType<TParams, TResult> requestType, MessageId id, TParams requestParams)
            => clientWriter.WriteMessage(Message.Request(id, requestType.MethodName, JToken.FromObject(requestParams)));

        /// <summary>
        /// Sends the client's response to a request the endpoint sent.
        /// </summary>
        public Task SendResponseAsync(Message request, object result)
            => clientWriter.WriteMessage(Message.Response(request.Id, request.Method, JToken.FromObject(result)));

        public void Dispose()
        {
            // Closing the client's end ends the message loop's read.
            clientToEndpoint.Dispose();
            Endpoint.MessageDispatcher.Stop();
            endpointInput.Dispose();
            endpointToClient.Dispose();
            clientInput.Dispose();
        }

        private sealed class StreamChannel : ChannelBase
        {
            public override Task WaitForConnection() => Task.CompletedTask;

            protected override void Initialize(IMessageSerializer messageSerializer, Stream inputStream = null, Stream outputStream = null)
            {
                MessageReader = new MessageReader(inputStream, messageSerializer);
                MessageWriter = new MessageWriter(outputStream, messageSerializer);
                IsConnected = true;
            }

            protected override void Shutdown()
            {
            }
        }
    }
}
