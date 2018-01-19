//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Dmp.Contracts.Hosting;
using Microsoft.SqlTools.Dmp.Hosting.Protocol;

namespace Microsoft.SqlTools.Dmp.Hosting
{
    /// <summary>
    /// Interface for service hosts. Inherits interface requirements for JSON RPC hosts
    /// </summary>
    public interface IServiceHost : IJsonRpcHost
    {
        /// <summary>
        /// Registers a task to be executed when the initialize event is received
        /// </summary>
        /// <param name="initializeCallback">Function to execute when the initialize event received</param>
        void RegisterInitializeTask(Func<InitializeParams, IEventSender, Task> initializeCallback);

        /// <summary>
        /// Registers a task to be executed when the shutdown event is received, before the channel
        /// is closed
        /// </summary>
        /// <param name="shutdownCallback">Function to execute when the shutdown request is received</param>
        void RegisterShutdownTask(Func<object, IEventSender, Task> shutdownCallback);
    }
}