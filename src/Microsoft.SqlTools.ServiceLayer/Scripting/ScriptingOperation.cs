//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    /// <summary>
    /// Base class for scripting operations.  Because scripting operations can be very long
    /// running, there my be multiple concurrent scripting operations.  To distinguish events
    /// between concurrent scripting operations, use the operation id.
    /// </summary>
    public abstract class ScriptingOperation : IDisposable
    {
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        protected ScriptingOperation()
        {
            this.OperationId = Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken { get { return this.cancellation.Token; } }

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// Excecutes the scripting operation.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Cancels the scripting operation.
        /// </summary>
        public virtual void Cancel()
        {
            if (this.cancellation != null && !this.cancellation.IsCancellationRequested)
            {
                Logger.Write(LogLevel.Verbose, string.Format("ScriptingOperation.Cancel invoked for OperationId {0}", this.OperationId));
                this.cancellation.Cancel();
            }
        }

        /// <summary>
        /// Disposes the scripting operation.
        /// </summary>
        public abstract void Dispose();
    }
}
