//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.Hosting.Protocol.Channel
{
    /// <summary>
    /// Defines a base implementation for servers and their clients over a
    /// single kind of communication channel.
    /// </summary>
    public abstract class ChannelBase
    {
        /// <summary>
        /// Gets a boolean that is true if the channel is connected or false if not.
        /// </summary>
        public bool IsConnected { get; protected set; }

        public Stream InputStream { get; protected set; }

        public Stream OutputStream { get; protected set; }

        /// <summary>
        /// Starts the channel and initializes its input and output streams.
        /// </summary>
        /// <param name="inputStream">Optional stream to use for the input stream</param>
        /// <param name="outputStream">Optional stream to use for the output stream</param>
        public void Start(Stream? inputStream = null, Stream? outputStream = null)
        {
            this.Initialize(inputStream, outputStream);
        }

        /// <summary>
        /// Returns a Task that allows the consumer of the ChannelBase
        /// implementation to wait until a connection has been made to
        /// the opposite endpoint whether it's a client or server.
        /// </summary>
        /// <returns>A Task to be awaited until a connection is made.</returns>
        public abstract Task WaitForConnection();

        /// <summary>
        /// Stops the channel.
        /// </summary>
        public void Stop()
        {
            this.Shutdown();
        }

        /// <summary>
        /// A method to be implemented by subclasses to handle the
        /// actual initialization of the channel and assignment of the
        /// input and output streams.
        /// </summary>
        /// <param name="inputStream">Optional stream to use for the input stream</param>
        /// <param name="outputStream">Optional stream to use for the output stream</param>
        protected abstract void Initialize(Stream? inputStream = null, Stream? outputStream = null);

        /// <summary>
        /// A method to be implemented by subclasses to handle shutdown
        /// of the channel once Stop is called.
        /// </summary>
        protected abstract void Shutdown();
    }
}
