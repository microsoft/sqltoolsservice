//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Base class for scripting operations.  Because scripting operations can be very long
    /// running, there my be multiple concurrent scripting operations.  To distinguish events
    /// between concurrent scripting operations, use the operation id.
    /// </summary>
    public abstract class ScriptingOperation : IDisposable
    {
        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; protected set; }

        /// <summary>
        /// Starts the Task which performs the scripting operation.
        /// </summary>
        public abstract Task Execute();

        /// <summary>
        /// Cancels the scripting operation.
        /// </summary>
        public abstract void Cancel();

        /// <summary>
        /// Disposes the scripting operation.
        /// </summary>
        public abstract void Dispose();
    }
}
