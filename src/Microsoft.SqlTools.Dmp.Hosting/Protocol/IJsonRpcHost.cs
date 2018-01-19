//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Dmp.Hosting.Protocol
{
    /// <summary>
    /// Interface for a JSON RPC host
    /// </summary>
    public interface IJsonRpcHost : IEventSender, IRequestSender, IMessageDispatcher
    {
        /// <summary>
        /// Starts the JSON RPC host 
        /// </summary>
        void Start();
        
        /// <summary>
        /// Stops the JSON RPC host
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Waits for the JSON RPC host to exit
        /// </summary>
        void WaitForExit();
    }
}