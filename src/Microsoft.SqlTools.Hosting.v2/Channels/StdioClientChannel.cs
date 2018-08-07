//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;

namespace Microsoft.SqlTools.Hosting.Channels
{
    /// <summary>
    /// Provides a client implementation for the standard I/O channel.
    /// Launches the server process and then attaches to its console
    /// streams.
    /// </summary>
    public class StdioClientChannel : ChannelBase
    {
        private readonly string serviceProcessPath;
        private readonly string serviceProcessArguments;

        private Stream inputStream;
        private Stream outputStream;
        private Process serviceProcess;

        /// <summary>
        /// Initializes an instance of the StdioClient.
        /// </summary>
        /// <param name="serverProcessPath">The full path to the server process executable.</param>
        /// <param name="serverProcessArguments">Optional arguments to pass to the service process executable.</param>
        public StdioClientChannel(string serverProcessPath, params string[] serverProcessArguments)
        {
            serviceProcessPath = serverProcessPath;

            if (serverProcessArguments != null)
            {
                serviceProcessArguments = string.Join(" ", serverProcessArguments);
            }
        }

        public int ProcessId { get; private set; }
        
        public override void Start()
        {
            serviceProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = serviceProcessPath,
                    Arguments = serviceProcessArguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            // Start the process
            serviceProcess.Start();
            ProcessId = serviceProcess.Id;

            // Open the standard input/output streams
            inputStream = serviceProcess.StandardOutput.BaseStream;
            outputStream = serviceProcess.StandardInput.BaseStream;

            // Set up the message reader and writer
            MessageReader = new MessageReader(inputStream);
            MessageWriter = new MessageWriter(outputStream);

            IsConnected = true;
        }

        public override Task WaitForConnection()
        {
            // We're always connected immediately in the stdio channel
            return Task.FromResult(true);
        }

        public override void Stop()
        {
            if (inputStream != null)
            {
                inputStream.Dispose();
                inputStream = null;
            }

            if (outputStream != null)
            {
                outputStream.Dispose();
                outputStream = null;
            }

            if (MessageReader != null)
            {
                MessageReader = null;
            }

            if (MessageWriter != null)
            {
                MessageWriter = null;
            }

            serviceProcess.Kill();
        }
    }
}
