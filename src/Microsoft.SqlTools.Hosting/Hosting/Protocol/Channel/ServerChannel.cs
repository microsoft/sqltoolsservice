//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Hosting.Protocol.Channel
{
    /// <summary>
    /// Provides a stream-based server channel for communicating with a client
    /// using the JSON-RPC message protocol.
    /// </summary>
    public class ServerChannel : ChannelBase
    {
        /// <summary>
        /// Initializes the channel with the given streams. If either stream is not provided,
        /// falls back to the console standard I/O streams and sets the console encoding to UTF-8.
        /// </summary>
        protected override void Initialize(Stream? inputStream = null, Stream? outputStream = null)
        {
#if !NanoServer
            // Ensure that the console is using UTF-8 encoding
            if (inputStream == null)
            {
                System.Console.InputEncoding = Encoding.UTF8;
            }
            if (outputStream == null)
            {
                System.Console.OutputEncoding = Encoding.UTF8;
            }
#endif

            // Open the standard input/output streams
            this.InputStream = inputStream ?? System.Console.OpenStandardInput();
            this.OutputStream = outputStream ?? System.Console.OpenStandardOutput();

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
