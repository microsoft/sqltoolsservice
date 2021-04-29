using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.SqlTools.Utility;

namespace Microsoft.AzureMonitor.ServiceLayer.Scripting
{
    /// <summary>
    /// Base class for scripting operations.  Because scripting operations can be very long
    /// running, there my be multiple concurrent scripting operations.  To distinguish events
    /// between concurrent scripting operations, use the operation id.
    /// </summary>
    public abstract class ScriptingOperation : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        protected ScriptingOperation()
        {
            this.OperationId = Guid.NewGuid().ToString();
        }

        protected CancellationToken CancellationToken => this.cancellation.Token;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; private set; }

        /// <summary>
        /// Executes the scripting operation.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Cancels the scripting operation.
        /// </summary>
        public virtual void Cancel()
        {
            if (!this.cancellation.IsCancellationRequested)
            {
                Logger.Write(TraceEventType.Verbose, string.Format("Cancel invoked for OperationId {0}", this.OperationId));
                this.cancellation.Cancel();
            }
        }

        /// <summary>
        /// Disposes the scripting operation.
        /// </summary>
        public abstract void Dispose();
    }
}