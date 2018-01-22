//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.DataProtocol.Hosting.Protocol;

namespace Microsoft.SqlTools.DataProtocol.Hosting.Channels
{
    /// <summary>
    /// Defines a base implementation for servers and their clients over a
    /// single kind of communication channel.
    /// </summary>
    public abstract class ChannelBase
    {
        #region Properties 
        
        /// <summary>
        /// Gets a boolean that is true if the channel is connected or false if not.
        /// </summary>
        public bool IsConnected { get; protected internal set; }

        /// <summary>
        /// Gets the MessageReader for reading messages from the channel.
        /// </summary>
        public MessageReader MessageReader { get; protected internal set; }

        /// <summary>
        /// Gets the MessageWriter for writing messages to the channel.
        /// </summary>
        public MessageWriter MessageWriter { get; protected internal set; }
        
        #endregion
        
        #region Abstract Methods

        /// <summary>
        /// Starts the channel and initializes the MessageDispatcher.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Stops the channel.
        /// </summary>
        public abstract void Stop();
        
        /// <summary>
        /// Returns a Task that allows the consumer of the ChannelBase
        /// implementation to wait until a connection has been made to
        /// the opposite endpoint whether it's a client or server.
        /// </summary>
        /// <returns>A Task to be awaited until a connection is made.</returns>
        public abstract Task WaitForConnection();
        
        #endregion
    }
}
