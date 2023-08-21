﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol.Serializers;

namespace Microsoft.SqlTools.Hosting.Protocol.Channel
{
    /// <summary>
    /// Provides a server implementation for the standard I/O channel.
    /// When started in a process, attaches to the console I/O streams
    /// to communicate with the client that launched the process.
    /// </summary>
    public class StdioServerChannel : ChannelBase
    {
        private Stream inputStream;
        private Stream outputStream;

        protected override void Initialize(IMessageSerializer messageSerializer, Stream? inputStream = null, Stream? outputStream = null)
        {
#if !NanoServer
            // Ensure that the console is using UTF-8 encoding
            System.Console.InputEncoding = Encoding.UTF8;
            System.Console.OutputEncoding = Encoding.UTF8;
#endif

            // Open the standard input/output streams
            this.inputStream = inputStream ?? System.Console.OpenStandardInput();
            this.outputStream = outputStream ?? System.Console.OpenStandardOutput();

            // Set up the reader and writer
            this.MessageReader = 
                new MessageReader(
                    this.inputStream,
                    messageSerializer);

            this.MessageWriter = 
                new MessageWriter(
                    this.outputStream,
                    messageSerializer);

            this.IsConnected = true;
        }

        public override Task WaitForConnection()
        {
            // We're always connected immediately in the stdio channel
            return Task.FromResult(true);
        }

        protected override void Shutdown()
        {
            // No default implementation needed, streams will be
            // disposed on process shutdown.
        }
    }
}
